using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Suportek.Services;

namespace Suportek
{
    /// <summary>
    /// Classe para armazenar dados do novo chamado durante o fluxo de conversação
    /// </summary>
    public class NovoChamadoDados
    {
        public string Nome { get; set; } = "";
        public string Email { get; set; } = "";
        public string TipoProblema { get; set; } = "";
        public string Descricao { get; set; } = "";
        public string Protocolo { get; set; } = "";
    }

    /// <summary>
    /// Página principal do aplicativo de chat de suporte da Auto Peças.
    /// Implementa uma interface de chat conversacional com opções de menu para diferentes tipos de suporte.
    /// </summary>
    /// <summary>
    /// Página principal do aplicativo de chat de suporte da Auto Peças.
    /// Implementa uma interface de chat conversacional com opções de menu para diferentes tipos de suporte.
    /// </summary>
    public partial class MainPage : ContentPage
    {
        /// <summary>
        /// Armazena o contexto atual da conversa para processar adequadamente as mensagens do usuário.
        /// Possíveis valores: "frete", "consultar_pedido", "cancelar_pedido", "pagamento", "duvidas_produto", 
        /// "confirmar_cancelamento_[numero_pedido]", "novo_chamado_nome", "novo_chamado_email", 
        /// "novo_chamado_tipo", "novo_chamado_descricao".
        /// </summary>
        private string currentContext = "";

        /// <summary>
        /// Armazena os dados do novo chamado durante o fluxo de conversação
        /// </summary>
        private NovoChamadoDados novoChamadoDados = new NovoChamadoDados();

        /// <summary>
        /// Cliente API para acesso ao backend
        /// </summary>
        private ApiClient _apiClient;

        /// <summary>
        /// Serviço de cálculo de frete da JadLog
        /// </summary>
        private Services.JadLogService _jadLogService;

        /// <summary>
        /// Inicializa uma nova instância da página principal.
        /// Exibe uma mensagem de boas-vindas do bot ao carregar a aplicação.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            _apiClient = new ApiClient();
            _jadLogService = new Services.JadLogService();
            InicializarBancoDeDados();
            AddBotMessage("Olá! Bem-vindo ao suporte da Auto Peças. Como posso ajudá-lo hoje?");
        }

        /// <summary>
        /// Inicializa o banco de dados criando as tabelas necessárias
        /// </summary>
        private async void InicializarBancoDeDados()
        {
            try
            {
                await _apiClient.CriarTabelaChamados();
                AddBotMessage($"✅ Conectado ao servidor: {ApiClient.GetBaseUrl()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar banco de dados: {ex.Message}");
                AddBotMessage("⚠️ Não foi possível conectar ao servidor de dados. Toque em 'Servidor' para configurar o endereço da API e tente novamente.");
            }
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
        /// Manipulador de evento para o botão "Novo Chamado".
        /// Inicia o fluxo de abertura de novo chamado solicitando o nome do usuário.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private void OnNovoChamadoClicked(object sender, EventArgs e)
        {
            currentContext = "novo_chamado_nome";
            novoChamadoDados = new NovoChamadoDados(); // Limpa dados anteriores
            AddUserMessage("Novo Chamado");
            AddBotMessage("Qual é o seu nome?");
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
            await SendMessage();
        }

        /// <summary>
        /// Manipulador de evento para quando o usuário pressiona Enter no campo de entrada.
        /// </summary>
        /// <param name="sender">O objeto que disparou o evento</param>
        /// <param name="e">Argumentos do evento</param>
        private async void OnMessageEntryCompleted(object sender, EventArgs e)
        {
            await SendMessage();
        }

        /// <summary>
        /// Envia a mensagem do usuário
        /// </summary>
        private async Task SendMessage()
        {
            string? message = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            AddUserMessage(message);
            MessageEntry.Text = string.Empty;

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
                    // Remove caracteres não numéricos do CEP
                    var cepLimpo = new string(input.Where(char.IsDigit).ToArray());
                    if (cepLimpo.Length == 8)
                    {
                        AddBotMessage($"✅ Consultando frete para o CEP {cepLimpo}...");
                        await CalcularFrete(cepLimpo);
                    }
                    else
                    {
                        AddBotMessage("❌ CEP inválido. Por favor, informe um CEP válido (8 dígitos).");
                    }
                    break;

                case "consultar_pedido":
                    AddBotMessage($"🔍 Consultando pedido #{input}...");
                    var consultaPedidoSucesso = await RegistrarConsultaPedido(input);
                    if (!consultaPedidoSucesso)
                    {
                        AddBotMessage("⚠️ Não consegui registrar a consulta de pedido. Verifique o servidor configurado e tente novamente.");
                        return;
                    }
                    await Task.Delay(300);
                    AddBotMessage($"📋 Status do Pedido #{input}:\n\nStatus: Em Separação\nÚltima atualização: Hoje às 14:30\nPrevisão de entrega: 5 dias úteis\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;

                case "cancelar_pedido":
                    var numeroPedidoCancelamento = input.Trim();
                    if (!string.IsNullOrEmpty(numeroPedidoCancelamento))
                    {
                        AddBotMessage($"⚠️ Você tem certeza que deseja cancelar o pedido #{numeroPedidoCancelamento}?");
                        AddBotMessage("Digite 'SIM' para confirmar ou 'NÃO' para cancelar:");
                        currentContext = "confirmar_cancelamento_" + numeroPedidoCancelamento;
                    }
                    else
                    {
                        AddBotMessage("❌ Por favor, informe um número de pedido válido.");
                    }
                    break;

                case var ctx when ctx.StartsWith("confirmar_cancelamento_"):
                    string pedidoNum = ctx.Replace("confirmar_cancelamento_", "");
                    if (input.Equals("SIM", StringComparison.OrdinalIgnoreCase))
                    {
                        var cancelamentoSucesso = await RegistrarCancelamentoPedido(pedidoNum, "solicitado");
                        if (!cancelamentoSucesso)
                        {
                            AddBotMessage("⚠️ Não consegui registrar o cancelamento. Verifique o servidor configurado e tente novamente.");
                            ShowOptionsPanel();
                            return;
                        }
                        AddBotMessage($"✅ Pedido #{pedidoNum} cancelado com sucesso!\n\nO estorno será realizado em até 7 dias úteis.\n\nPosso ajudar em mais alguma coisa?");
                    }
                    else
                    {
                        AddBotMessage("Cancelamento não realizado. Posso ajudar em mais alguma coisa?");
                    }
                    ShowOptionsPanel();
                    break;

                case "pagamento":
                    if (string.IsNullOrWhiteSpace(input))
                    {
                        AddBotMessage("❌ Por favor, descreva o problema de pagamento.");
                        return;
                    }
                    var pagamentoSucesso = await RegistrarProblemaPagamento(input);
                    if (!pagamentoSucesso)
                    {
                        AddBotMessage("⚠️ Não consegui registrar o problema de pagamento. Verifique o servidor configurado e tente novamente.");
                        ShowOptionsPanel();
                        return;
                    }
                    AddBotMessage($"✅ Problema registrado com sucesso!\n\n\"{input}\"\n\n📞 Nossa equipe financeira entrará em contato em até 24 horas.\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;

                case "duvidas_produto":
                    var duvidaSucesso = await RegistrarDuvidaProduto(input);
                    if (!duvidaSucesso)
                    {
                        AddBotMessage("⚠️ Não consegui registrar sua dúvida. Verifique o servidor configurado e tente novamente.");
                        return;
                    }
                    AddBotMessage($"Sobre sua dúvida: \"{input}\"\n\n💡 Nossa equipe técnica pode fornecer informações mais detalhadas. Você pode:\n\n- Ligar: (11) 3333-4444\n- WhatsApp: (11) 99999-8888\n- E-mail: suporte@autopecas.com.br\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    break;

                case "novo_chamado_nome":
                    novoChamadoDados.Nome = input;
                    currentContext = "novo_chamado_email";
                    AddBotMessage("Qual é o seu e-mail?");
                    break;

                case "novo_chamado_email":
                    novoChamadoDados.Email = input;
                    currentContext = "novo_chamado_tipo";
                    AddBotMessage("Qual é o tipo de problema? (pagamento, produto, entrega, outro)");
                    break;

                case "novo_chamado_tipo":
                    novoChamadoDados.TipoProblema = input;
                    currentContext = "novo_chamado_descricao";
                    AddBotMessage("Por favor, descreva o problema de forma resumida:");
                    break;

                case "novo_chamado_descricao":
                    novoChamadoDados.Descricao = input;
                    // Gera protocolo único
                    string protocolo = GerarProtocolo();
                    novoChamadoDados.Protocolo = protocolo;
                    
                    // Salva no banco de dados
                    var chamadoSucesso = await SalvarChamadoNoBanco(novoChamadoDados);
                    if (!chamadoSucesso)
                    {
                        AddBotMessage("⚠️ Não consegui registrar o chamado. Verifique o servidor configurado e tente novamente.");
                        Console.WriteLine($"[APP] Falha ao registrar chamado: {novoChamadoDados.Protocolo} - {novoChamadoDados.Nome}");
                        ShowOptionsPanel();
                        return;
                    }
                    
                    AddBotMessage($"✅ Chamado aberto! Seu protocolo é {protocolo}.\n\nNossa equipe entrará em contato em até 24 horas.\n\nPosso ajudar em mais alguma coisa?");
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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var grid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) }
                    }
                };

                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#CC0000"),
                    CornerRadius = 12,
                    Padding = 10,
                    HorizontalOptions = LayoutOptions.End,
                    HasShadow = false
                };

                frame.Content = new Label
                {
                    Text = message,
                    TextColor = Colors.White,
                    FontSize = 14
                };

                Grid.SetColumn(frame, 1);
                grid.Children.Add(frame);
                ChatContainer.Children.Add(grid);
                _ = ScrollToBottom();
            });
        }

        /// <summary>
        /// Adiciona uma mensagem do bot ao chat com estilo visual específico.
        /// As mensagens do bot aparecem alinhadas à esquerda com fundo cinza.
        /// </summary>
        /// <param name="message">O texto da mensagem a ser exibida</param>
        private void AddBotMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var grid = new Grid
                {
                    Margin = new Thickness(0, 0, 0, 8),
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) },
                        new ColumnDefinition { Width = new GridLength(0.25, GridUnitType.Star) }
                    }
                };

                var frame = new Frame
                {
                    BackgroundColor = Color.FromArgb("#555555"),
                    CornerRadius = 12,
                    Padding = 10,
                    HorizontalOptions = LayoutOptions.Start,
                    HasShadow = false
                };

                frame.Content = new Label
                {
                    Text = message,
                    TextColor = Colors.White,
                    FontSize = 14
                };

                Grid.SetColumn(frame, 0);
                grid.Children.Add(frame);
                ChatContainer.Children.Add(grid);
                _ = ScrollToBottom();
            });
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

        /// <summary>
        /// Gera um protocolo único para o chamado
        /// </summary>
        /// <returns>Protocolo no formato CH202511061530</returns>
        private string GerarProtocolo()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomSuffix = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"CH{timestamp}{randomSuffix}";
        }

        /// <summary>
        /// Salva o chamado no banco de dados
        /// </summary>
        /// <param name="dados">Dados do chamado a ser salvo</param>
        private async Task<bool> SalvarChamadoNoBanco(NovoChamadoDados dados)
        {
            try
            {
                Console.WriteLine($"[APP] ========== SALVANDO CHAMADO ==========");
                Console.WriteLine($"[APP] Protocolo: {dados.Protocolo}");
                Console.WriteLine($"[APP] Nome: {dados.Nome}");
                Console.WriteLine($"[APP] Email: {dados.Email}");
                Console.WriteLine($"[APP] TipoProblema: {dados.TipoProblema}");
                Console.WriteLine($"[APP] Descricao: {dados.Descricao ?? "(vazio)"}");
                
                // Valida dados antes de enviar
                if (string.IsNullOrWhiteSpace(dados.Protocolo) || 
                    string.IsNullOrWhiteSpace(dados.Nome) || 
                    string.IsNullOrWhiteSpace(dados.Email) || 
                    string.IsNullOrWhiteSpace(dados.TipoProblema))
                {
                    Console.WriteLine($"[APP] ❌ DADOS INVÁLIDOS - Campos obrigatórios vazios!");
                    return false;
                }
                
                bool sucesso = await _apiClient.SalvarChamado(
                    dados.Protocolo, 
                    dados.Nome, 
                    dados.Email, 
                    dados.TipoProblema, 
                    dados.Descricao ?? string.Empty
                );
                
                if (!sucesso)
                {
                    Console.WriteLine($"[APP] ❌ Falha ao salvar chamado - método retornou false");
                }
                else
                {
                    Console.WriteLine($"[APP] ✅ ✅ ✅ SUCESSO! Chamado {dados.Protocolo} salvo!");
                }
                
                return sucesso;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[APP] ❌ EXCEÇÃO ao salvar chamado: {ex.GetType().Name}");
                Console.WriteLine($"[APP] Mensagem: {ex.Message}");
                if (ex.StackTrace != null)
                {
                    Console.WriteLine($"[APP] Stack: {ex.StackTrace}");
                }
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[APP] Inner: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Calcula o frete usando o serviço da JadLog diretamente (sem depender da API web)
        /// </summary>
        /// <param name="cep">CEP de destino</param>
        private async Task CalcularFrete(string cep)
        {
            try
            {
                Console.WriteLine($"[MainPage] Calculando frete para CEP: {cep}");
                
                // Usa o JadLogService diretamente, igual funciona na web
                var freteResponse = await _jadLogService.CalcularFreteAsync(cep);
                
                if (freteResponse == null)
                {
                    Console.WriteLine($"[MainPage] Resposta nula do JadLogService");
                    AddBotMessage("❌ Não foi possível calcular o frete.\n\nPor favor, verifique o CEP informado ou tente novamente.\n\nPosso ajudar em mais alguma coisa?");
                    ShowOptionsPanel();
                    return;
                }

                Console.WriteLine($"[MainPage] Resposta recebida - Sucesso: {freteResponse.Sucesso}, Serviços: {freteResponse.Servicos?.Count ?? 0}, Erro: {freteResponse.Erro}");
                
                if (freteResponse.Sucesso && freteResponse.Servicos != null && freteResponse.Servicos.Count > 0)
                {
                    var mensagem = "✅ Frete calculado:\n\n";
                    foreach (var servico in freteResponse.Servicos)
                    {
                        // Ícones para os serviços da JadLog
                        var icone = servico.Nome.Contains("Expresso") ? "🚀" 
                                  : servico.Nome.Contains("Rodoviário") ? "🚛" 
                                  : "📦"; // Econômico ou outros
                        mensagem += $"{icone} {servico.Nome}: R$ {servico.Valor:F2} ({servico.PrazoEntrega} dias úteis)\n";
                    }
                    mensagem += "\nPosso ajudar em mais alguma coisa?";
                    AddBotMessage(mensagem);
                }
                else
                {
                    var erro = !string.IsNullOrWhiteSpace(freteResponse.Erro) 
                        ? freteResponse.Erro 
                        : "Não foi possível calcular o frete. Verifique o CEP informado ou tente novamente mais tarde.";
                    
                    Console.WriteLine($"[MainPage] Erro ao calcular frete: {erro}");
                    AddBotMessage($"❌ {erro}\n\nPosso ajudar em mais alguma coisa?");
                }
                ShowOptionsPanel();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Exceção ao calcular frete: {ex.Message}");
                Console.WriteLine($"[MainPage] Stack trace: {ex.StackTrace}");
                AddBotMessage($"❌ Erro ao calcular frete: {ex.Message}\n\nPor favor, verifique o CEP informado e tente novamente.\n\nPosso ajudar em mais alguma coisa?");
                ShowOptionsPanel();
            }
        }

        /// <summary>
        /// Registra uma consulta de pedido no banco de dados
        /// </summary>
        /// <param name="numeroPedido">Número do pedido consultado</param>
        private async Task<bool> RegistrarConsultaPedido(string numeroPedido)
        {
            try
            {
                bool sucesso = await _apiClient.RegistrarConsultaPedido(numeroPedido);
                if (!sucesso)
                {
                    Console.WriteLine("Erro ao registrar consulta de pedido");
                }
                return sucesso;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar consulta de pedido: {ex}");
                await MostrarErroAsync($"Não foi possível registrar a consulta de pedido.\nDetalhes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registra um cancelamento de pedido no banco de dados
        /// </summary>
        /// <param name="numeroPedido">Número do pedido cancelado</param>
        /// <param name="status">Status do cancelamento</param>
        private async Task<bool> RegistrarCancelamentoPedido(string numeroPedido, string status)
        {
            try
            {
                bool sucesso = await _apiClient.RegistrarCancelamentoPedido(numeroPedido, status);
                if (!sucesso)
                {
                    Console.WriteLine("Erro ao registrar cancelamento de pedido");
                }
                return sucesso;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar cancelamento de pedido: {ex}");
                await MostrarErroAsync($"Não foi possível registrar o cancelamento.\nDetalhes: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Registra um problema de pagamento no banco de dados
        /// </summary>
        /// <param name="descricao">Descrição do problema</param>
        private async Task<bool> RegistrarProblemaPagamento(string descricao)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(descricao))
                {
                    Console.WriteLine("Descrição do problema de pagamento está vazia");
                    return false;
                }

                Console.WriteLine($"[MainPage] Registrando problema de pagamento: {descricao}");
                bool sucesso = await _apiClient.RegistrarProblemaPagamento(descricao);
                
                if (!sucesso)
                {
                    Console.WriteLine("[MainPage] Registro de problema de pagamento retornou false");
                }
                else
                {
                    Console.WriteLine("[MainPage] Problema de pagamento registrado com sucesso");
                }
                
                return sucesso;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[MainPage] Erro HTTP ao registrar problema de pagamento: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[MainPage] Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Erro ao registrar problema de pagamento: {ex}");
                Console.WriteLine($"[MainPage] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Registra uma dúvida sobre produto no banco de dados
        /// </summary>
        /// <param name="descricao">Descrição da dúvida</param>
        private async Task<bool> RegistrarDuvidaProduto(string descricao)
        {
            try
            {
                bool sucesso = await _apiClient.RegistrarDuvidaProduto(descricao);
                if (!sucesso)
                {
                    Console.WriteLine("Erro ao registrar dúvida sobre produto");
                }
                return sucesso;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar dúvida sobre produto: {ex}");
                await MostrarErroAsync($"Não foi possível registrar a dúvida.\nDetalhes: {ex.Message}");
                return false;
            }
        }

        private async Task MostrarErroAsync(string mensagem)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Erro", mensagem, "OK"));
            }
            catch
            {
                // Ignora erros ao mostrar alertas
            }
        }

        private async void OnConfigurarServidorClicked(object sender, EventArgs e)
        {
            string baseAtual = ApiClient.GetBaseUrl();
            string? novaBase = await DisplayPromptAsync(
                "Configurar servidor",
                "Informe a URL base da API (exemplo: http://192.168.0.10:5099/). Deixe em branco para usar o padrão.",
                accept: "Salvar",
                cancel: "Cancelar",
                initialValue: baseAtual,
                keyboard: Keyboard.Url);

            if (novaBase == null)
            {
                return; // usuário cancelou
            }

            ApiClient.SetBaseUrl(string.IsNullOrWhiteSpace(novaBase) ? null : novaBase);
            _apiClient = new ApiClient();

            AddBotMessage($"🔄 Endereço do servidor atualizado para: {ApiClient.GetBaseUrl()}");
            await InicializarNovamenteAsync();
        }

        private async Task InicializarNovamenteAsync()
        {
            try
            {
                await _apiClient.CriarTabelaChamados();
                AddBotMessage($"✅ Conectado ao servidor: {ApiClient.GetBaseUrl()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar banco após atualizar servidor: {ex.Message}");
                AddBotMessage("⚠️ Ainda não consegui conectar ao servidor. Verifique se a API está em execução e tente novamente.");
            }
        }

    }
}