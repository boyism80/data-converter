using ExcelTableConverter.Configuration;
using System.ComponentModel.DataAnnotations;

namespace ExcelTableConverter
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var config = AppConfiguration.Parse(args);
                if (Logger.TTY)
                    Console.Clear();

                return new Converter(config).Run() ? 0 : 1;
            }
            catch (ValidationException e)
            {
                Logger.Error($"Configuration validation failed: {e.Message}");
                return 1;
            }
            catch (ArgumentException e)
            {
                Logger.Error($"Invalid arguments: {e.Message}");
                return 1;
            }
        }
    }
}
