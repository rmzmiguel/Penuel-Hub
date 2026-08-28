using System.Net.Mail;
using System.Text;

namespace Penuel.Bootstrap;

/// <summary>Lectura interactiva de la terminal para el arranque.</summary>
internal static class ConsoleInput
{
    /// <summary>
    /// Se lanza cuando ya no hay entrada que leer. Este programa pide una contraseña,
    /// así que EXIGE una terminal interactiva: en un pipe o en CI no tiene sentido y
    /// debe abortar en vez de quedarse girando o reventar leyendo teclas.
    /// </summary>
    public sealed class NoInteractiveInputException(string message) : Exception(message);

    public static void RequireInteractiveTerminal()
    {
        if (Console.IsInputRedirected)
        {
            throw new NoInteractiveInputException(
                "Este programa necesita una terminal interactiva porque pide una contraseña " +
                "sin mostrarla en pantalla. Ejecútalo directamente, no a través de una tubería " +
                "ni desde un proceso automatizado.");
        }
    }

    /// <summary>Mismos límites que <c>CreateUserAccountCommandValidator</c> (Sección 8.1).</summary>
    public const int MinPasswordLength = 8;
    public const int MaxPasswordBytes = 72;

    public static string ReadEmail(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var line = Console.ReadLine();

            if (line is null)
            {
                throw new NoInteractiveInputException("Se acabó la entrada antes de recibir un correo.");
            }

            var value = line.Trim();

            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("  El correo es obligatorio.");
                continue;
            }

            if (!MailAddress.TryCreate(value, out _))
            {
                Console.WriteLine("  Ese correo no tiene un formato válido.");
                continue;
            }

            return value;
        }
    }

    /// <summary>
    /// Lee una contraseña sin mostrarla en pantalla y pidiéndola dos veces.
    /// El valor nunca se escribe en disco ni en un log: solo se usa para generar el hash BCrypt.
    /// </summary>
    public static string ReadPasswordTwice()
    {
        while (true)
        {
            var first = ReadMasked("Contraseña (mínimo 8 caracteres): ");

            if (first.Length < MinPasswordLength)
            {
                Console.WriteLine($"  Debe tener al menos {MinPasswordLength} caracteres.");
                continue;
            }

            if (Encoding.UTF8.GetByteCount(first) > MaxPasswordBytes)
            {
                // BCrypt trunca en silencio más allá de 72 bytes.
                Console.WriteLine($"  No puede exceder {MaxPasswordBytes} bytes.");
                continue;
            }

            var second = ReadMasked("Confírmala:                       ");

            if (!string.Equals(first, second, StringComparison.Ordinal))
            {
                Console.WriteLine("  Las contraseñas no coinciden. Intenta de nuevo.");
                continue;
            }

            return first;
        }
    }

    public static bool Confirm(string prompt)
    {
        Console.Write(prompt);
        var answer = (Console.ReadLine() ?? string.Empty).Trim();
        return answer.Equals("SI", StringComparison.OrdinalIgnoreCase)
            || answer.Equals("SÍ", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadMasked(string prompt)
    {
        Console.Write(prompt);
        var builder = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return builder.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
                Console.Write('*');
            }
        }
    }
}
