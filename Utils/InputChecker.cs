namespace Banksim_Web.Utils
{
    public class InputChecker
    {
        public static decimal VerificarDecimal(string mensagem)
        {
            decimal valor;
            while (true)
            {
                Console.Write(mensagem);
                if (decimal.TryParse(Console.ReadLine(), out valor) && valor >= 0)
                    return valor;

                Console.WriteLine("Insira um valor numérico válido!");
            }
        }
        public static string VerificarString(string mensagem)
        {
            string? entrada;
            while (true)
            {
                Console.Write(mensagem);
                entrada = Console.ReadLine();
                if (!string.IsNullOrEmpty(entrada))
                    return entrada;

                    Console.WriteLine("Entrada inválida!");
            }
        }
        public static int VerificarInt(string mensagem)
        {
            int valor;
            while (true)
            {
                Console.Write(mensagem);
                if (int.TryParse(Console.ReadLine(), out valor) && valor >= 0)
                    return valor;

                Console.WriteLine("Insira um valor numérico válido!");
            }
        }
    }
}
