# Dashboard de Notificações WhatsApp

Um dashboard ASP.NET Core para visualizar notificações WhatsApp enviadas aos cooperados sobre pedidos de compra.

## Recursos

- Dashboard de página única que atualiza automaticamente a cada 1 minuto
- Exibe pedidos de venda e seus respectivos pedidos de compra para cooperados
- Mostra o status das respostas dos cooperados via WhatsApp
- Interface responsiva e compatível com todos os navegadores modernos, incluindo TVs com acesso à internet
- Agrupamento de pedidos por data de entrega
- Contadores visuais para diferentes status de pedidos

## Requisitos

- .NET 7.0 ou superior
- PostgreSQL
- Kubernetes para implantação

## Configuração

A aplicação se conecta ao PostgreSQL usando a string de conexão definida no arquivo `appsettings.json`. Para o ambiente de desenvolvimento, você pode modificar a string de conexão no arquivo `appsettings.Development.json`.

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=omieleads;Username=postgres;Password=postgres;Trust Server Certificate=true"
}
```

Em produção, a aplicação espera que o PostgreSQL esteja disponível como um serviço no mesmo cluster Kubernetes com o nome `postgres-service`.

## Deployment no Kubernetes

1. Construa a imagem Docker:

```bash
docker build -t whatsapp-notification-dashboard:latest .
```

2. Aplique o manifesto Kubernetes:

```bash
kubectl apply -f kubernetes-manifest.yaml
```

3. Acesse o dashboard através do Ingress configurado em `/notifications`

## Uso

A aplicação é totalmente automatizada e não requer interação do usuário. O dashboard exibirá:

- Contadores para diferentes status de pedidos (Atendidos, Parciais, Não Atendidos, Pendentes)
- Uma tabela com todos os pedidos para os próximos 3 dias, agrupados por data de entrega
- Atualização automática a cada minuto

## Personalização

O intervalo de atualização pode ser modificado no arquivo `appsettings.json`, alterando o valor de `RefreshInterval` (em milissegundos).