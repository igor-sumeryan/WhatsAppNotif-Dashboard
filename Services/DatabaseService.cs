using Npgsql;
using WhatsAppNotificationDashboard.Models;

namespace WhatsAppNotificationDashboard.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public async Task<List<NotificationModel>> GetNotificationsAsync()
        {
            var notifications = new List<NotificationModel>();

            using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        sales.""SalesOrderNumber"" AS ""Código do Pedido de Venda"",
                        sales.""ShippingDate"" AS ""Data de Entrega do Pedido de Venda"",
                        cliente.""Name"" AS ""Nome do Cliente"",
                        porders.""PurchaseOrderNumber"" AS ""Código do Pedido de Compra"",
                        fornecedor.""Name"" AS ""Nome do Cooperado"",
                        porders.""MessageSent"" - INTERVAL '3 hour' AS ""Data e Hora do Envio da Mensagem"",
                        CASE WHEN porders.""MessageReplyWith"" = 0 THEN 'Pedido Atendido Integralmente'
                             WHEN porders.""MessageReplyWith"" = 1 THEN 'Pedido Atendido Parcialmente'
                             WHEN porders.""MessageReplyWith"" = 2 THEN 'Pedido Não Atendido'
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

            return notifications;
        }
    }
}