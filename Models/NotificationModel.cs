namespace WhatsAppNotificationDashboard.Models
{
    public class NotificationModel
    {
        public string CodigoPedidoVenda { get; set; } = string.Empty;
        public DateTime DataEntregaPedidoVenda { get; set; }
        public string NomeCliente { get; set; } = string.Empty;
        public string CodigoPedidoCompra { get; set; } = string.Empty;
        public string NomeCooperado { get; set; } = string.Empty;
        public DateTime? DataHoraEnvioMensagem { get; set; }
        public string StatusAtendimentoPedido { get; set; } = string.Empty;
    }
}