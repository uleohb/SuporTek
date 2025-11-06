using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel; // necessário para MainThread
using System;
using System.Threading.Tasks;

namespace AutoPecasChat
{
    /// <summary>
    /// Página principal do aplicativo de chat de suporte da Auto Peças.
    /// Implementa uma interface de chat conversacional com opções de menu para diferentes tipos de suporte.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Armazena o contexto atual da conversa para processar adequadamente as mensagens do usuário.
        /// Possíveis valores: "frete", "consultar_pedido", "cancelar_pedido", "pagamento", "duvidas_produto", 
        /// ou "confirmar_cancelamento_[numero_pedido]".
        /// </summary>
        private string currentContext = "";

        /// <summary>
        /// Inicializa uma nova instância da página principal.
        /// Exibe uma mensagem de boas-vindas do bot ao carregar a aplicação.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            AddBotMessage("Olá! Bem-vindo ao suporte da Auto Peças. Como posso ajudá-lo hoje?");
        }

        /// <summary>
        /// Manipulador de evento para o botão "Consultar Frete".
        /// Inicia o fluxo de consulta de frete solicitando o CEP do usuário.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnConsultarFreteClicked(object sender, EventArgs e)
        {
            currentContext = "frete";
            AddUserMessage("Consultar Frete");
            AddBotMessage("Para consultar o frete, por favor informe seu CEP:");
            ShowInputPanel();
        }

        /// <summary>
        /// Manipulador de evento para o botão "Consultar Pedido".
        /// Inicia o fluxo de consulta de pedido solicitando o número do pedido.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnConsultarPedidoClicked(object sender, EventArgs e)
        {
            currentContext = "consultar_pedido";
            AddUserMessage("Consultar Pedido");
            AddBotMessage("Para consultar seu pedido, por favor informe o número do pedido:");
            ShowInputPanel();
        }

        /// <summary>
        /// Manipulador de evento para o botão "Cancelar Pedido".
        /// Inicia o fluxo de cancelamento de pedido solicitando o número do pedido.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnCancelarPedidoClicked(object sender, EventArgs e)
        {
            currentContext = "cancelar_pedido";
            AddUserMessage("Cancelar Pedido");
            AddBotMessage("Para cancelar seu pedido, por favor informe o número do pedido:");
            ShowInputPanel();
        }

        /// <summary>
        /// Manipulador de evento para o botão "Problemas com Pagamento".
        /// Inicia o fluxo de atendimento para problemas de pagamento.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnProblemasPagamentoClicked(object sender, EventArgs e)
        {
            currentContext = "pagamento";
            AddUserMessage("Problemas com Pagamento");
            AddBotMessage("Entendo que você está com problemas no pagamento. Por favor, descreva o problema:");
            ShowInputPanel();
        }

        /// <summary>
        /// Manipulador de evento para o botão "Dúvidas sobre Produto".
        /// Inicia o fluxo de atendimento para dúvidas sobre produtos.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnDuvidasProdutoClicked(object sender, EventArgs e)
        {
            currentContext = "duvidas_produto";
            AddUserMessage("Dúvidas sobre Produto");
            AddBotMessage("Estou aqui para ajudar! Qual é a sua dúvida sobre o produto?");
            ShowInputPanel();
        }

        /// <summary>
        /// Manipulador de evento para o botão "Enviar" da caixa de texto.
        /// Processa a mensagem digitada pelo usuário e inicia o processamento da resposta.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            string? message = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            AddUserMessage(message);
            MessageEntry.Text = string.Empty;

            await Task.Delay(400); // pequena pausa simulando processamento
            await ProcessUserInput(message);
        }

        /// <summary>
        /// Processa a entrada do usuário baseada no contexto atual da conversa.
        /// Implementa diferentes fluxos de conversação para cada tipo de suporte.
        /// </summary>
        /// <param name="input">A mensagem digitada pelo usuário</param>
        /// <returns>Task representando a operação assíncrona</returns>
        private async Task ProcessUserInput(string input)
        {
            switch (currentContext)
            {
                case "frete":
                    if (input.Length == 8 || input.Length == 9)
                    {
                        AddBotMessage($"✅ Consultando frete para o CEP {input}...");
                        await Task.Delay(1000);
                        AddBotMessage($"Frete calculado:\n\n📦 PAC: R$ 25,00 (5-7 dias úteis)\n🚀 SEDEX: R$ 45,00 (2-3 dias úteis)\n\nPosso ajudar em mais alguma coisa?");
                        ShowOptionsPanel();
                    }
                    else
                    {
                        AddBotMessage("❌ CEP inválido. Por favor, informe um CEP válido (somente números).");
                    }
                    break;

                case "consultar_pedido":
                    AddBotMessage($"🔍 Consultando pedido #{input}...");
                    await Task.Delay(1000);
                    AddBotMessage($"📋 Status do Pedido #{input}:\n\nStatus: Em Separação\nÚltima atualização: Hoje às 14:30\nPrevisão de entrega: 5 dias úteis\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;

                case "cancelar_pedido":
                    AddBotMessage($"⚠️ Você tem certeza que deseja cancelar o pedido #{input}?");
                    AddBotMessage("Digite 'SIM' para confirmar ou 'NÃO' para cancelar:");
                    currentContext = "confirmar_cancelamento_" + input;
                    break;

                case var ctx when ctx.StartsWith("confirmar_cancelamento_"):
                    string pedidoNum = ctx.Replace("confirmar_cancelamento_", "");
                    if (input.Equals("SIM", StringComparison.OrdinalIgnoreCase))
                    {
                        AddBotMessage($"✅ Pedido #{pedidoNum} cancelado com sucesso!\n\nO estorno será realizado em até 7 dias úteis.\n\nPosso ajudar em mais alguma coisa?");
                    }
                    else
                    {
                        AddBotMessage("Cancelamento não realizado. Posso ajudar em mais alguma coisa?");
                    }
                    ShowOptionsPanel();
                    break;

                case "pagamento":
                    AddBotMessage($"Entendi. Vou registrar o seguinte problema:\n\n\"{input}\"\n\n📞 Nossa equipe financeira entrará em contato em até 24 horas.\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;

                case "duvidas_produto":
                    AddBotMessage($"Sobre sua dúvida: \"{input}\"\n\n💡 Nossa equipe técnica pode fornecer informações mais detalhadas. Você pode:\n\n- Ligar: (11) 3333-4444\n- WhatsApp: (11) 99999-8888\n- E-mail: suporte@autopecas.com.br\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;
            }
        }

        /// <summary>
        /// Adiciona uma mensagem do usuário ao chat com estilo visual específico.
        /// As mensagens do usuário aparecem alinhadas à direita com fundo vermelho.
        /// </summary>
        /// <param name="message">O texto da mensagem a ser exibida</param>
        private void AddUserMessage(string message)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#CC0000"),
                CornerRadius = 15,
                Padding = 10,
                Margin = new Thickness(50, 5, 5, 5),
                HorizontalOptions = LayoutOptions.End
            };

            frame.Content = new Label
            {
                Text = message,
                TextColor = Colors.White,
                FontSize = 14
            };

            ChatContainer.Children.Add(frame);
            _ = ScrollToBottom();
        }

        /// <summary>
        /// Adiciona uma mensagem do bot ao chat com estilo visual específico.
        /// As mensagens do bot aparecem alinhadas à esquerda com fundo cinza.
        /// </summary>
        /// <param name="message">O texto da mensagem a ser exibida</param>
        private void AddBotMessage(string message)
        {
            var frame = new Frame
            {
                BackgroundColor = Color.FromArgb("#555555"),
                CornerRadius = 15,
                Padding = 10,
                Margin = new Thickness(5, 5, 50, 5),
                HorizontalOptions = LayoutOptions.Start
            };

            frame.Content = new Label
            {
                Text = message,
                TextColor = Colors.White,
                FontSize = 14
            };

            ChatContainer.Children.Add(frame);
            _ = ScrollToBottom();
        }

        /// <summary>
        /// Exibe o painel de entrada de texto e oculta o painel de opções.
        /// Usado quando o usuário precisa digitar informações específicas.
        /// </summary>
        private void ShowInputPanel()
        {
            OptionsPanel.IsVisible = false;
            InputPanel.IsVisible = true;
        }

        /// <summary>
        /// Exibe o painel de opções de menu e oculta o painel de entrada.
        /// Reseta o contexto da conversa para permitir nova seleção de opção.
        /// </summary>
        private void ShowOptionsPanel()
        {
            InputPanel.IsVisible = false;
            OptionsPanel.IsVisible = true;
            currentContext = "";
        }

        /// <summary>
        /// Faz scroll automático para o final do chat quando uma nova mensagem é adicionada.
        /// Garante que a mensagem mais recente sempre esteja visível.
        /// </summary>
        /// <returns>Task representando a operação assíncrona</returns>
        private async Task ScrollToBottom()
        {
            await Task.Delay(100);
            if (ChatScrollView != null && ChatContainer != null)
            {
                await ChatScrollView.ScrollToAsync(0, ChatContainer.Height, true);
            }
        }
    }
}