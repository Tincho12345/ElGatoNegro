IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Consultas] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(120) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [Telefono] nvarchar(40) NULL,
    [Mensaje] nvarchar(2000) NOT NULL,
    [FechaAlta] datetime2 NOT NULL,
    [Leida] bit NOT NULL,
    CONSTRAINT [PK_Consultas] PRIMARY KEY ([Id])
);

CREATE TABLE [Trabajos] (
    [Id] int NOT NULL IDENTITY,
    [Titulo] nvarchar(160) NOT NULL,
    [Descripcion] nvarchar(2000) NULL,
    [FechaAlta] datetime2 NOT NULL,
    [Publicado] bit NOT NULL,
    CONSTRAINT [PK_Trabajos] PRIMARY KEY ([Id])
);

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [NombreUsuario] nvarchar(80) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(400) NOT NULL,
    [Rol] nvarchar(40) NOT NULL,
    [Activo] bit NOT NULL,
    [FechaAlta] datetime2 NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);

CREATE TABLE [Medios] (
    [Id] int NOT NULL IDENTITY,
    [TrabajoId] int NOT NULL,
    [Tipo] int NOT NULL,
    [RutaRelativa] nvarchar(400) NOT NULL,
    [TextoAlternativo] nvarchar(200) NULL,
    [Orden] int NOT NULL,
    [FechaAlta] datetime2 NOT NULL,
    CONSTRAINT [PK_Medios] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Medios_Trabajos_TrabajoId] FOREIGN KEY ([TrabajoId]) REFERENCES [Trabajos] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [UsuarioId] int NOT NULL,
    [Token] nvarchar(200) NOT NULL,
    [Expira] datetime2 NOT NULL,
    [FechaAlta] datetime2 NOT NULL,
    [Revocado] datetime2 NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_Consultas_FechaAlta] ON [Consultas] ([FechaAlta]);

CREATE INDEX [IX_Medios_TrabajoId] ON [Medios] ([TrabajoId]);

CREATE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);

CREATE INDEX [IX_RefreshTokens_UsuarioId] ON [RefreshTokens] ([UsuarioId]);

CREATE INDEX [IX_Trabajos_Publicado] ON [Trabajos] ([Publicado]);

CREATE UNIQUE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);

CREATE UNIQUE INDEX [IX_Usuarios_NombreUsuario] ON [Usuarios] ([NombreUsuario]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260901130019_InicialTrabajosWeb', N'10.0.11');

COMMIT;
GO

