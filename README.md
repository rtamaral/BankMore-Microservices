# 🏦 BankMore API

Uma API completa para sistema bancário desenvolvida com .NET 8, implementando funcionalidades de contas correntes, transferências e processamento assíncrono com Apache Kafka.

## 🚀 Tecnologias Utilizadas

- **🔧 .NET 8** - Framework principal
- **🗄️ SQL Server** - Banco de dados principal
- **📨 Apache Kafka** - Sistema de mensageria para processamento assíncrono
- **🐳 Docker** - Containerização e orquestração
- **🔍 Dapper** - ORM para acesso aos dados
- **📋 MediatR** - Padrão CQRS e mediação
- **🔐 JWT** - Autenticação e autorização
- **📊 Swagger** - Documentação da API
- **📝 FluentValidation** - Validação de entrada
- **🧪 xUnit** - Testes unitários

## 🏗️ Arquitetura

O projeto segue os princípios de **Clean Architecture** e **CQRS**:

```
📁 BankMore.Api/
├── 🎯 Controllers/          # Endpoints da API
├── 📋 Application/          # Lógica de aplicação
│   ├── Commands/           # Comandos (Write)
│   ├── Queries/           # Consultas (Read)
│   ├── Handlers/          # Manipuladores CQRS
│   └── DTOs/              # Objetos de transferência
├── 🏗️ Infrastructure/       # Acesso a dados e serviços externos
│   ├── Database/          # Configuração do banco
│   ├── Repositories/      # Repositórios
│   └── Messaging/         # Kafka Producers/Consumers
└── 🌐 Domain/              # Entidades e regras de negócio
```

## 🔧 Pré-requisitos

### Ferramentas Necessárias
- **.NET 8 SDK** ou superior
- **SQL Server** (LocalDB ou instância completa)
- **Apache Kafka** + **Zookeeper**
- **Docker Desktop** (opcional, mas recomendado)
- **Visual Studio 2022** ou **VS Code**

### 📨 Configuração do Apache Kafka

#### 1. **Instalação do Kafka**
1. Baixe o Apache Kafka em: https://kafka.apache.org/downloads
2. Extraia na unidade C: criando a pasta: `C:\kafka`

#### 2. **Configuração do Zookeeper**
1. Navegue até a pasta do Kafka: `C:\kafka`
2. Execute o Zookeeper:
```bash
.\bin\windows\zookeeper-server-start.bat .\config\zookeeper.properties
```

#### 3. **Inicialização do Kafka**
Em um novo terminal, execute:
```bash
.\bin\windows\kafka-server-start.bat .\config\server.properties
```

#### 4. **Criação dos Tópicos** (Opcional)
```bash
# Tópico para transferências realizadas
.\bin\windows\kafka-topics.bat --create --topic transferencias-realizadas --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1

# Tópico para tarifas realizadas
.\bin\windows\kafka-topics.bat --create --topic tarifas-realizadas --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1
```

### 🐳 Alternativa com Docker

Se preferir usar Docker, execute:

```bash
# Docker Compose para Kafka + Zookeeper
docker-compose up -d
```

Arquivo `docker-compose.yml`:
```yaml
version: '3.8'
services:
  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    ports:
      - "2181:2181"
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181

  kafka:
    image: confluentinc/cp-kafka:latest
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://localhost:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    depends_on:
      - zookeeper
```

## ⚙️ Configuração do Projeto

### 1. **Clone o repositório**
```bash
git clone <url-do-repositorio>
cd bankmore-api
```

### 2. **Configuração da Connection String**
Edite o arquivo `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BankMoreDb;Trusted_Connection=true;"
  },
  "Jwt": {
    "Secret": "sua-chave-secreta-super-segura-aqui-123456"
  }
}
```

### 3. **Criação do Banco de Dados**
```bash
# Executar scripts SQL para criar tabelas
sqlcmd -S (localdb)\mssqllocaldb -i database-setup.sql
```

### 4. **Instalação das dependências**
```bash
dotnet restore
```

### 5. **Execução do projeto**
```bash
dotnet run
```

## 🚀 Executando o Sistema Completo

## 🚀 Executando o Sistema Completo

### **Ordem de Inicialização:**

1. **🗄️ SQL Server** (deve estar rodando)
2. **📨 Zookeeper:**
```bash
cd C:\kafka
.\bin\windows\zookeeper-server-start.bat .\config\zookeeper.properties
```

3. **📨 Kafka Server:**
```bash
cd C:\kafka
.\bin\windows\kafka-server-start.bat .\config\server.properties
```

4. **🏦 BankMore API:**
```bash
dotnet run
```

### **📊 Acessar o Swagger:**
```
http://localhost:5000
```

## 🔌 Endpoints Principais

### 🔐 **Autenticação**
- `POST /api/auth/register` - Registrar nova conta
- `POST /api/auth/login` - Fazer login

### 💰 **Conta Corrente**
- `GET /api/account/{accountId}/balance` - Consultar saldo

### 💸 **Transferências**
- `POST /api/transfer` - Realizar transferência (processa tarifa via Kafka)

## 📨 Fluxo de Mensageria Kafka

### **🔄 Arquitetura de Eventos:**

1. **Transferência Solicitada** →
2. **Kafka Producer** publica no tópico `transferencias-realizadas` →
3. **TarifaHandler** consome a mensagem →
4. **Débito da tarifa** no banco de dados →
5. **Kafka Producer** publica no tópico `tarifas-realizadas` →
6. **Confirmação da tarifa** processada

### **📋 Tópicos Kafka:**
- `transferencias-realizadas` - Transferências para processar tarifas
- `tarifas-realizadas` - Tarifas processadas com sucesso

## 🧪 Executando Testes

```bash
# Todos os testes
dotnet test

# Testes com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

## 🔒 Segurança

- **JWT Authentication** - Tokens com expiração configurável
- **Autorização baseada em roles**
- **Validação de entrada** com FluentValidation
- **Logs auditáveis** de todas as operações

## 📦 Pacotes NuGet Principais

```xml
<!-- Core Framework -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="Swashbuckle.AspNetCore" />

<!-- Banco de Dados -->
<PackageReference Include="Dapper" />
<PackageReference Include="Microsoft.Data.SqlClient" />

<!-- CQRS e Mediação -->
<PackageReference Include="MediatR" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" />

<!-- Kafka -->
<PackageReference Include="KafkaFlow" />
<PackageReference Include="KafkaFlow.Serializer.JsonCore" />
<PackageReference Include="KafkaFlow.Microsoft.DependencyInjection" />
<PackageReference Include="KafkaFlow.LogHandler.Console" />

<!-- Validação -->
<PackageReference Include="FluentValidation" />

<!-- Testes -->
<PackageReference Include="xunit" />
<PackageReference Include="Moq" />
```

## 🚨 Troubleshooting

### **Kafka não conecta:**
- Verifique se o Zookeeper está rodando primeiro
- Confirme que as portas 2181 e 9092 estão livres
- Execute os comandos na pasta correta: `C:\kafka`

### **Erro de Connection String (SQL Server + SSMS v20):**
- Verifique se o SQL Server está rodando
- Teste a conexão diretamente no **SSMS v20**
- Confirme se o banco `BankMoreDb` foi criado
- Verifique se a instância LocalDB está ativa: `SqlLocalDB info`
- Para SQL Server Express, use: `localhost\SQLEXPRESS`

### **JWT inválido:**
- Verifique a chave secreta no `appsettings.json`
- Confirme se o token não expirou
- Use o endpoint `/auth/login` para obter um novo token

### **🔄 Migração para Oracle:**
Se estiver migrando para Oracle e encontrar problemas:
- Verifique se o Oracle Client está instalado
- Confirme se o serviço Oracle está rodando na porta 1521
- Teste a conexão com SQL*Plus ou Oracle SQL Developer
- Ajuste as queries para sintaxe Oracle (diferenças em TOP, LIMIT, etc.)

## 📝 Observações Técnicas

### **🎯 Escolha do SQL Server:**
O projeto foi desenvolvido com **SQL Server** e **Dapper** por questões de:
- ✅ **Facilidade de instalação** (LocalDB incluso no Visual Studio)
- ✅ **Configuração simplificada** para desenvolvimento local
- ✅ **Menor curva de aprendizado** para a equipe
- ✅ **Documentação abundante** e suporte da comunidade

### **🏢 Para Ambientes Corporativos:**
Em cenários enterprise que exigem **Oracle Database**, recomenda-se:
- **NHibernate** como ORM principal (melhor suporte Oracle)
- **Oracle.ManagedDataAccess.Core** para conectividade
- **FluentNHibernate** para mapeamentos fluent
- Revisão das queries e stored procedures

### **⚖️ Comparação de ORMs:**

| Aspecto | Dapper (SQL Server) | NHibernate (Oracle) |
|---------|-------------------|-------------------|
| **Performance** | ⚡ Muito rápida | 🔄 Boa com caching |
| **Simplicidade** | 🎯 Muito simples | 📚 Mais complexo |
| **Suporte Oracle** | ⚠️ Limitado | ✅ Excelente |
| **Recursos Avançados** | ❌ Básico | ✅ Completo |
| **Curva de Aprendizado** | 📈 Baixa | 📈 Alta |

## 👥 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'Add some AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo `LICENSE.md` para detalhes.

## 📞 Contato

- **Desenvolvedor:** Robson Amaral
- **Email:** robsontrindade016@gmail.com
- **LinkedIn:** https://www.linkedin.com/in/robson-amaral-a2457075/

---

⚡ **BankMore API** - Sistema bancário moderno com processamento assíncrono via Apache Kafka 🚀
