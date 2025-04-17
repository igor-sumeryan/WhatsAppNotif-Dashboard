using Npgsql;
using System.Timers;
using WhatsAppNotificationDashboard.Models;

namespace WhatsAppNotificationDashboard.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly ILogger<DatabaseService> _logger;
        private System.Timers.Timer _connectionTimer;
        private bool _isConnected = false;
        private DateTime _lastConnectionAttempt;
        private string _connectionError = string.Empty;

        public bool IsConnected => _isConnected;
        public string ConnectionError => _connectionError;
        public DateTime LastConnectionAttempt => _lastConnectionAttempt;

        public event EventHandler<bool> ConnectionStateChanged;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
            _logger = logger;

            // Configurar o timer para verificar a conexão a cada 30 segundos
            _connectionTimer = new System.Timers.Timer(30000); // 30 segundos
            _connectionTimer.Elapsed += async (sender, e) => await CheckDatabaseConnectionAsync();
            _connectionTimer.AutoReset = true;
            _connectionTimer.Start();

            // Fazer a verificação inicial
            Task.Run(async () => await CheckDatabaseConnectionAsync());
        }

        private async Task CheckDatabaseConnectionAsync()
        {
            _lastConnectionAttempt = DateTime.Now;
            bool previousState = _isConnected;
            _connectionError = string.Empty;

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    
                    // Executa uma consulta simples para testar a conexão
                    using (var cmd = new NpgsqlCommand("SELECT 1", connection))
                    {
                        await cmd.ExecuteScalarAsync();
                    }
                    
                    _isConnected = true;
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _connectionError = ex.Message;
                _logger.LogError(ex, "Falha ao conectar ao banco de dados");
            }

            // Notifica mudanças no estado da conexão
            if (previousState != _isConnected)
            {
                ConnectionStateChanged?.Invoke(this, _isConnected);
            }
        }

        public async Task<List<NotificationModel>> GetNotificationsAsync()
        {
            var notifications = new List<NotificationModel>();

            if (!_isConnected)
            {
                // Se não estiver conectado, retorna uma lista vazia
                return notifications;
            }

            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var query = @"
                        SELECT
                            sales.""SalesOrderNumber"" AS ""Código do Pedido de Venda"",
                            sales.""ShippingDate"" AS ""Data de Entrega do Pedido de Venda"",
                            cliente.""LegalName"" AS ""Nome do Cliente"",
                            porders.""PurchaseOrderNumber"" AS ""Código do Pedido de Compra"",
                            fornecedor.""Name"" AS ""Nome do Cooperado"",
                            porders.""MessageSent"" - INTERVAL '3 hour' AS ""Data e Hora do Envio da Mensagem"",
                            CASE WHEN porders.""MessageReplyWith"" = 1 THEN 'Pedido Atendido Integralmente'
                                 WHEN porders.""MessageReplyWith"" = 2 THEN 'Pedido Atendido Parcialmente'
                                 WHEN porders.""MessageReplyWith"" = 3 THEN 'Pedido Não Atendido'
                                 ELSE '...' END AS ""Status do Atendimento do Pedido""
                        FROM
                            public.omie_salesorders AS sales
                                INNER JOIN public.omie_purchaseorders AS porders ON porders.""SalesOrderCode"" = sales.""SalesOrderCode""
                                INNER JOIN public.omie_persons AS fornecedor ON fornecedor.""Code"" = porders.""Cooperated""
                                INNER JOIN public.omie_persons AS cliente ON cliente.""Code"" = sales.""Client""
                        WHERE
                            sales.""ShippingDate""::date BETWEEN now()::date AND (now()::date+ INTERVAL '3 day')
                        ORDER BY
                            sales.""ShippingDate"",
                            CASE WHEN porders.""MessageSent"" IS NULL THEN 1 ELSE 0 END,
                            porders.""SalesOrderCode"",
                            porders.""PurchaseOrderCode""";

                    using (var command = new NpgsqlCommand(query, connection))
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            notifications.Add(new NotificationModel
                            {
                                CodigoPedidoVenda = reader["Código do Pedido de Venda"].ToString()!,
                                DataEntregaPedidoVenda = Convert.ToDateTime(reader["Data de Entrega do Pedido de Venda"]),
                                NomeCliente = reader["Nome do Cliente"].ToString()!,
                                CodigoPedidoCompra = reader["Código do Pedido de Compra"].ToString()!,
                                NomeCooperado = reader["Nome do Cooperado"].ToString()!,
                                DataHoraEnvioMensagem = reader["Data e Hora do Envio da Mensagem"] as DateTime?,
                                StatusAtendimentoPedido = reader["Status do Atendimento do Pedido"].ToString()!
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _connectionError = ex.Message;
                _logger.LogError(ex, "Erro ao obter notificações");
                
                // Força uma nova verificação de conexão
                await CheckDatabaseConnectionAsync();
            }

            return notifications;
        }
    }
}