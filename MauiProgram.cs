using Microsoft.Extensions.Logging;

namespace Suportek
{
    /// <summary>
    /// Classe principal responsável pela configuração e inicialização do aplicativo .NET MAUI.
    /// Define os serviços, fontes e configurações necessárias para o funcionamento da aplicação.
    /// </summary>
    public static class MauiProgram
    {
        /// <summary>
        /// Cria e configura a aplicação MAUI com todos os serviços necessários.
        /// </summary>
        /// <returns>Uma instância configurada de MauiApp</returns>
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    // Configuração das fontes personalizadas do aplicativo
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            // Adiciona logging de debug apenas em builds de desenvolvimento
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

