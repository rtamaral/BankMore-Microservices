--Tabelas pertencentes na estrutura do projeto da API
CREATE TABLE contacorrente (
    idcontacorrente UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- GUID
    numero INT NOT NULL UNIQUE, -- número da conta corrente
    nome NVARCHAR(100) NOT NULL, -- nome do titular
    ativo BIT NOT NULL DEFAULT 0, -- (0 = inativa, 1 = ativa)
    senha NVARCHAR(200) NOT NULL, -- hash da senha
    salt NVARCHAR(200) NOT NULL -- salt usado no hash
);

CREATE TABLE movimento (
    idmovimento UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- identificador único
    idcontacorrente UNIQUEIDENTIFIER NOT NULL, -- conta vinculada
    datamovimento DATETIME NOT NULL, -- data/hora do movimento
    tipomovimento CHAR(1) NOT NULL CHECK (tipomovimento IN ('C','D')), -- C = Crédito, D = Débito
    valor DECIMAL(18,2) NOT NULL, -- valor com 2 casas decimais
    FOREIGN KEY (idcontacorrente) REFERENCES contacorrente(idcontacorrente)
);

CREATE TABLE tarifa (
    idtarifa UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- identificador único
    idcontacorrente UNIQUEIDENTIFIER NOT NULL, -- referência para conta
    datamovimento DATETIME NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (idcontacorrente) REFERENCES contacorrente(idcontacorrente)
);

CREATE TABLE transferencia (
    idtransferencia UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(), -- identificador único
    idcontacorrente_origem UNIQUEIDENTIFIER NOT NULL, -- conta origem
    idcontacorrente_destino UNIQUEIDENTIFIER NOT NULL, -- conta destino
    datamovimento DATETIME NOT NULL,
    valor DECIMAL(18,2) NOT NULL,
    FOREIGN KEY (idcontacorrente_origem) REFERENCES contacorrente(idcontacorrente),
    FOREIGN KEY (idcontacorrente_destino) REFERENCES contacorrente(idcontacorrente)
);

CREATE TABLE idempotencia (
    chave_idempotencia UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    requisicao NVARCHAR(MAX) NULL,
    resultado NVARCHAR(MAX) NULL
);