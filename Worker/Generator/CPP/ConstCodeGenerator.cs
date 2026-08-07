using ExcelTableConverter.Configuration;
using ExcelTableConverter.Factory.CPP;
using ExcelTableConverter.Model;
using ExcelTableConverter.Util;
using Scriban;
using System.Text.RegularExpressions;

namespace ExcelTableConverter.Worker.Generator.CPP
{
    public class ConstCodeGenerator : ParallelWorker<uint, string>
    {
        private static readonly Template _template = Template.Parse(File.ReadAllText($"Template/C++/const.txt"));
        private static readonly Template _luaTemplate = Template.Parse(File.ReadAllText($"Template/C++/const_lua.txt"));
        private static readonly Regex _enumValueRegex = new Regex(@"^([A-Z_][A-Z0-9_]*)\s*::\s*([A-Z_][A-Z0-9_]*)$", RegexOptions.Compiled);

        public Dictionary<uint, string> Declaration { get; private set; } = new Dictionary<uint, string>();
        public string LuaCode { get; private set; }

        public ConstCodeGenerator(Context ctx) : base(ctx)
        {
        }

        protected override IEnumerable<uint> OnReady()
        {
            foreach (var (scope, _) in Context.Configuration.DefinedScopes)
            {
                yield return scope;
            }
        }

        protected override IEnumerable<string> OnWork(uint scope)
        {
            var items = new Dictionary<string, List<object>>();
            foreach (var (groupName, constSet) in Context.Completed.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values.Where(x => AppConfiguration.ContainsScope(x.Scope, scope)))
                {
                    props.Add(new
                    {
                        Name = constData.Name,
                        Type = new TypeFactory(Context).Build(constData.Type),
                        Value = new AllocateValueFactory(Context).Build(constData.Type, constData.Value),
                    });
                }

                if (props.Count == 0)
                    continue;

                items.Add(groupName, props);
            }

            var obj = new ScribanEx
            {
                ["items"] = items,
                ["config"] = Context.Configuration,
            };
            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);
            yield return _template.Render(ctx);
        }

        protected override void OnWorked(uint input, string output, int percent)
        {
            Declaration.Add(input, output);
            base.OnWorked(input, output, percent);
        }

        protected override IReadOnlyList<string> OnFinish(IReadOnlyList<string> output)
        {
            GenerateLuaCode();
            return base.OnFinish(output);
        }

        private void GenerateLuaCode()
        {
            var constTables = new List<object>();
            foreach (var (groupName, constSet) in Context.Completed.Const.OrderBy(x => x.Key))
            {
                var props = new List<object>();
                foreach (var constData in constSet.Values)
                {
                    var typeInfo = BuildTypeInfo(constData.Type, constData.Value);
                    var prop = new
                    {
                        Name = constData.Name,
                        Type = new TypeFactory(Context).Build(constData.Type),
                        RawType = constData.Type,
                        TypeInfo = typeInfo,
                    } as object;

                    props.Add(prop);
                }

                if (props.Count == 0)
                    continue;

                constTables.Add(new
                {
                    Name = groupName,
                    Props = props
                } as object);
            }

            var obj = new ScribanEx
            {
                ["const_tables"] = constTables,
                ["config"] = Context.Configuration,
            };

            var ctx = ScribanEx.CreateContext();
            ctx.PushGlobal(obj);
            LuaCode = _luaTemplate.Render(ctx);
        }

        private object BuildTypeInfo(string type, object value)
        {
            // nullable 처리
            var isNullable = Util.Type.IsNullable(type);
            var isNullValue = isNullable && value == null;

            if (isNullable && !isNullValue)
            {
                // nullable이지만 값이 있는 경우, 내부 타입으로 재귀
                var innerType = type.Substring(0, type.Length - 1); // '?' 제거
                return new
                {
                    IsNullable = true,
                    IsNullValue = false,
                    InnerType = BuildTypeInfo(innerType, value)
                };
            }

            if (isNullable && isNullValue)
            {
                return new
                {
                    IsNullable = true,
                    IsNullValue = true,
                };
            }

            // reference 처리
            var isReference = type.StartsWith("$");
            if (isReference)
            {
                return BuildTypeInfo(Context.Completed.Schema.GetRootTableType(type), value);
            }

            // array 처리
            if (Util.Type.IsArray(type, out var arrayElementType))
            {
                return new
                {
                    IsArray = true,
                    ArrayElement = BuildTypeInfo(arrayElementType, null)
                };
            }

            // map 처리
            if (Util.Type.IsMap(type, out var mapKv))
            {
                return new
                {
                    IsMap = true,
                    MapKeyType = BuildTypeInfo(mapKv.Key, null),
                    MapValueType = BuildTypeInfo(mapKv.Value, null)
                };
            }

            // 기본 타입 처리
            var rootType = Context.Completed.Schema.GetRootTableType(type);
            var nakedType = Util.Type.Nake(rootType);
            var isEnum = Context.Completed.Enum.ContainsKey(nakedType);

            string[] stringTypes = { "string", "cron" };
            string[] numericTypes = { "int", "int32", "int32_t", "uint", "uint32", "uint32_t", "long", "int64", "int64_t", "ulong", "uint64", "uint64_t", "short", "int16", "int16_t", "ushort", "uint16", "uint16_t", "byte", "uint8", "uint8_t", "sbyte", "int8", "int8_t", "float", "double" };

            // enum 값 추출
            string enumValue = null;
            if (isEnum && value != null)
            {
                if (value is string strValue && Context.Completed.Enum[nakedType].ContainsKey(strValue))
                {
                    enumValue = strValue;
                }
                else
                {
                    // C++ 코드에서 enum 이름 추출 시도
                    var allocatedValue = new AllocateValueFactory(Context).Build(nakedType, value);
                    var match = _enumValueRegex.Match(allocatedValue);
                    if (match.Success)
                    {
                        var extractedEnumName = match.Groups[1].Value;
                        var extractedEnumValue = match.Groups[2].Value;

                        if (extractedEnumName == nakedType ||
                            extractedEnumName.EndsWith(nakedType) ||
                            Context.Completed.Enum.ContainsKey(extractedEnumName))
                        {
                            enumValue = extractedEnumValue;
                        }
                    }
                }
            }

            return new
            {
                IsBool = nakedType == "bool",
                IsString = stringTypes.Contains(nakedType),
                IsNumeric = numericTypes.Contains(nakedType),
                IsEnum = isEnum,
                EnumName = isEnum ? nakedType : null,
                EnumValue = enumValue,
                IsDateTime = nakedType == "datetime" || nakedType == "DateTime",
                IsTimeSpan = nakedType == "timespan" || nakedType == "TimeSpan",
                IsDateRange = nakedType == "date_range" || nakedType == "DateRange",
                IsPoint = Util.Type.IsPoint(nakedType, out _),
                IsRange = Util.Type.IsRange(nakedType, out _),
                IsSize = Util.Type.IsSize(nakedType, out _),
                IsArea = Util.Type.IsArea(nakedType, out _),
                IsDsl = Util.Type.IsDSL(type, out _, out _),
                NakedType = nakedType,
            };
        }

        private string ExtractEnumValue(ConstData constData, bool isEnum, string enumName)
        {
            if (!isEnum)
                return null;

            // const 값이 문자열인 경우 (enum 이름, 예: "ATTACK")
            if (constData.Value is string strValue)
            {
                // enum에 해당 이름이 있는지 확인
                if (Context.Completed.Enum[enumName].ContainsKey(strValue))
                {
                    return strValue;
                }
            }

            // const 값이 정수인 경우, enum 값으로 변환 필요
            // 하지만 정수로는 enum 이름을 알 수 없으므로 null 반환
            // 템플릿에서 정수로 처리
            // 또는 생성된 C++ 코드 문자열에서 enum 이름 추출 시도
            var allocatedValue = new AllocateValueFactory(Context).Build(constData.Type, constData.Value);
            var match = _enumValueRegex.Match(allocatedValue);
            if (match.Success)
            {
                var extractedEnumName = match.Groups[1].Value;
                var extractedEnumValue = match.Groups[2].Value;

                // enum 이름이 일치하는지 확인
                if (extractedEnumName == enumName ||
                    extractedEnumName.EndsWith(enumName) ||
                    Context.Completed.Enum.ContainsKey(extractedEnumName))
                {
                    return extractedEnumValue;
                }
            }

            return null;
        }
    }
}
