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

- .NET 8.0
- PostgreSQL 16
- Docker e Docker Compose (para desenvolvimento local)
- Kubernetes (para implantação em produção)

## Configuração

### Configuração Local

A aplicação se conecta ao PostgreSQL usando a string de conexão definida no arquivo `appsettings.json`. Para o ambiente de desenvolvimento, você pode modificar a string de conexão no arquivo `appsettings.Development.json`.

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=bpo;Username=postgres;Password=suasenha;SSL Mode=Disable"
}
```

### Desenvolvimento com Docker Compose

Para executar a aplicação localmente com Docker Compose:

```bash
# Construir e iniciar os contêineres
docker-compose up -d

# Verificar logs
docker-compose logs -f

# Parar os contêineres
docker-compose down
```

O Docker Compose configurará automaticamente a aplicação e um banco de dados PostgreSQL, e fará a comunicação entre eles.

### Configuração em Produção

Em produção, a aplicação espera que o PostgreSQL esteja disponível como um serviço no mesmo cluster Kubernetes com o nome `postgres-service`. A string de conexão pode ser configurada através de variáveis de ambiente ou ConfigMaps do Kubernetes.

## Deployment no Kubernetes

### Resumo de passos para implantação no Kubernetes

1. **Preparar a imagem e enviá-la para um registro de contêineres**

   ```bash
   # Para Docker Hub
   docker login
   docker tag whatsapp-notification-dashboard:latest seuusuario/whatsapp-notification-dashboard:latest
   docker push seuusuario/whatsapp-notification-dashboard:latest
   
   # Para Google Container Registry (GCR)
   gcloud auth configure-docker
   docker tag whatsapp-notification-dashboard:latest gcr.io/seuprojeto/whatsapp-notification-dashboard:latest
   docker push gcr.io/seuprojeto/whatsapp-notification-dashboard:latest
   
   # Para Amazon ECR
   aws ecr get-login-password --region sua-regiao | docker login --username AWS --password-stdin seu-id-conta.dkr.ecr.sua-regiao.amazonaws.com
   docker tag whatsapp-notification-dashboard:latest seu-id-conta.dkr.ecr.sua-regiao.amazonaws.com/whatsapp-notification-dashboard:latest
   docker push seu-id-conta.dkr.ecr.sua-regiao.amazonaws.com/whatsapp-notification-dashboard:latest
   ```

2. **Editar o manifesto Kubernetes**

   - Substitua `${REGISTRY_URL}` no arquivo kubernetes-manifest.yaml pelo URL do seu registro:
     - Para Docker Hub: `seuusuario`
     - Para GCR: `gcr.io/seuprojeto`
     - Para ECR: `seu-id-conta.dkr.ecr.sua-regiao.amazonaws.com`
   
   - Ajuste o nome do host no Ingress rule para seu domínio real
   - Ajuste o `storageClassName` do PVC para corresponder ao disponibilizado pelo seu cluster

3. **Configurar o acesso ao seu cluster Kubernetes**

   ```bash
   # Para GKE (Google)
   gcloud container clusters get-credentials nome-do-cluster --zone zona --project seuprojeto
   
   # Para EKS (AWS)
   aws eks update-kubeconfig --region sua-regiao --name nome-do-cluster
   
   # Para AKS (Azure)
   az aks get-credentials --resource-group grupo-recursos --name nome-do-cluster
   ```

4. **Implantar no cluster Kubernetes**

   ```bash
   # Verificar o acesso ao cluster
   kubectl get nodes
   
   # Criar um namespace (opcional, mas recomendado)
   kubectl create namespace whatsapp-dashboard
   
   # Aplicar o manifesto principal (deployment)
   kubectl apply -f kubernetes-manifest.yaml -n whatsapp-dashboard
   
   # Aplicar o serviço apropriado
   kubectl apply -f kubernetes-service.yaml -n whatsapp-dashboard
   
   # Verificar os recursos criados
   kubectl get pods,services,deployments -n whatsapp-dashboard
   ```

   ### Opções de Serviço Kubernetes

   O projeto inclui três tipos diferentes de arquivos de serviço para expor a aplicação:

   1. **kubernetes-service.yaml** - Serviço ClusterIP (apenas interno)
      - Expõe a aplicação apenas dentro do cluster
      - Ideal para comunicação entre microsserviços
      - Comando: `kubectl apply -f kubernetes-service.yaml`

   2. **kubernetes-service-nodeport.yaml** - Serviço NodePort
      - Expõe a aplicação em uma porta fixa (30080) em cada nó do cluster
      - Útil para ambientes de desenvolvimento ou quando você não tem um balanceador de carga
      - Comando: `kubectl apply -f kubernetes-service-nodeport.yaml`

   3. **kubernetes-service-loadbalancer.yaml** - Serviço LoadBalancer
      - Provisiona automaticamente um balanceador de carga externo quando executado em um provedor de nuvem
      - Ideal para ambientes de produção
      - Comando: `kubectl apply -f kubernetes-service-loadbalancer.yaml`

   Escolha o tipo de serviço mais adequado para o seu ambiente.

5. **Verificar o status dos recursos**

   ```bash
   # Verificar os pods (devem estar "Running")
   kubectl get pods -n whatsapp-dashboard
   
   # Ver logs de um pod específico
   kubectl logs -f -n whatsapp-dashboard deployment/whatsapp-notification-dashboard
   
   # Verificar o ingress para obter o endereço externo
   kubectl get ingress -n whatsapp-dashboard
   ```

6. **Configurar DNS (se estiver usando um domínio personalizado)**

   Após o Ingress estar provisionado, você receberá um endereço IP ou hostname. Configure seu domínio para apontar para este endereço.

Os manifestos Kubernetes incluem:
- **Secret** - para armazenar credenciais do PostgreSQL
- **ConfigMap** - para configurações da aplicação
- **Deployments** - para a aplicação e PostgreSQL
- **Services** - para expor os serviços internamente
- **PersistentVolumeClaim** - para armazenamento do PostgreSQL
- **Ingress** - para expor a aplicação externamente

### Configuração da porta da aplicação

A aplicação está configurada para rodar na porta **8080** dentro do contêiner Docker. Isso é importante para:

1. Garantir compatibilidade com alguns ambientes de cluster Kubernetes
2. Evitar conflitos com outras aplicações que possam estar usando a porta 80
3. Resolver problemas relacionados a permissões em alguns sistemas operacionais

Se você precisar alterar esta porta, certifique-se de ajustar:
- A variável de ambiente `ASPNETCORE_URLS` no Dockerfile e docker-compose.yml
- A porta exposta no Dockerfile (`EXPOSE 8080`)
- O mapeamento de portas no docker-compose.yml e kubernetes-manifest.yaml
- As configurações de probes (liveness, readiness, startup) no kubernetes-manifest.yaml

### Avisos de proteção de dados

Quando executada em um contêiner, a aplicação pode exibir os seguintes avisos:

```
warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
      Storing keys in a directory '/root/.aspnet/DataProtection-Keys' that may not be persisted outside of the container.
warn: Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager[35]
      No XML encryptor configured. Key may be persisted to storage in unencrypted form.
```

Estes são apenas avisos e não afetam o funcionamento da aplicação. Eles indicam que:

1. As chaves de proteção de dados são armazenadas no contêiner e serão perdidas quando o contêiner for recriado
2. As chaves não são criptografadas

Para ambientes de produção onde a persistência de chaves é importante, você pode:

1. Montar um volume persistente no diretório `/root/.aspnet/DataProtection-Keys`
2. Ou adicionar a variável de ambiente `ASPNETCORE_DataProtection__DisableAutomaticKeyGeneration=true` para desabilitar os avisos

### Solução de problemas com arquitetura ARM vs x86_64

O erro `exec /usr/bin/dotnet: exec format error` ocorre quando você tenta executar um contêiner Docker construído para uma arquitetura diferente da arquitetura do host onde está tentando executá-lo. Neste caso, você compilou o contêiner em um MacOS com processador ARM (Apple Silicon/M1/M2), mas está tentando executá-lo em um cluster Kubernetes que provavelmente usa processadores x86_64 (AMD/Intel).

Aqui está como resolver esse problema:

#### 1. Construa uma imagem multi-arquitetura

A solução é criar uma imagem Docker multi-arquitetura (também chamada de multi-plataforma ou multi-arch) que possa ser executada em ambas as arquiteturas ARM e x86_64:

```bash
# Configurar o buildx (se ainda não estiver configurado)
docker buildx create --name multiarch --use

# Construir e enviar a imagem multi-arquitetura para seu registro
docker buildx build --platform linux/amd64,linux/arm64 -t seuusuario/whatsapp-notification-dashboard:latest --push .
```

#### 2. Modificar o Dockerfile para garantir compatibilidade

Atualize o Dockerfile para garantir que ele seja compilado corretamente para múltiplas arquiteturas:

```dockerfile
# Especificar a plataforma explicitamente (útil para builds multi-arquitetura)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# resto do Dockerfile continua igual...
```

## Uso

A aplicação é totalmente automatizada e não requer interação do usuário. O dashboard exibirá:

- Contadores para diferentes status de pedidos (Atendidos, Parciais, Não Atendidos, Pendentes)
- Uma tabela com todos os pedidos para os próximos 3 dias, agrupados por data de entrega
- Atualização automática a cada minuto

## Personalização

O intervalo de atualização pode ser modificado no arquivo `appsettings.json`, alterando o valor de `RefreshInterval` (em milissegundos).

## Recursos de Resiliência

A aplicação inclui recursos para garantir alta resiliência:

- **Verificação de Conexão:** Verifica automaticamente a conexão com o banco de dados a cada 30 segundos
- **Reconexão Automática:** Tenta reconectar-se ao banco quando a conexão é perdida
- **Feedback Visual:** Mostra o status da conexão na interface, incluindo códigos de cores (verde=conectado, vermelho=desconectado)
- **Operação Contínua:** Continua funcionando mesmo quando o banco de dados está temporariamente indisponível
- **Mensagens Amigáveis:** Exibe mensagens claras ao usuário quando ocorrem problemas de conexão

## Contribuição

1. Faça um fork do projeto
2. Crie sua branch de feature (`git checkout -b feature/nova-funcionalidade`)
3. Faça commit das suas alterações (`git commit -am 'Adiciona nova funcionalidade'`)
4. Faça push para a branch (`git push origin feature/nova-funcionalidade`)
5. Crie um novo Pull Request

## Licença

Este projeto está licenciado sob a licença MIT - veja o arquivo LICENSE para detalhes.

## Suporte

Desenvolvido e mantido por M&B - Powered by Sumeryan.