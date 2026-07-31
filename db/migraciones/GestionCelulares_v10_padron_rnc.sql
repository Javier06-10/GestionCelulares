/* =============================================================================
   v10) Padrón de RNC de la DGII (consulta offline de contribuyentes).
        Importa el archivo DGII_RNC.TXT (descargado de la DGII, pipe-delimited,
        Windows-1252, CRLF) a dbo.PadronRnc para auto-completar la razón social
        al capturar un RNC/cédula.

        Requiere ejecutar como sysadmin (BULK INSERT) y que SQL Server pueda
        leer la ruta del archivo. Ajusta la ruta de BULK INSERT si es necesario.
   ============================================================================= */
USE [GestionCelulares];
GO

IF OBJECT_ID('dbo.PadronRncStaging') IS NOT NULL DROP TABLE dbo.PadronRncStaging;
CREATE TABLE dbo.PadronRncStaging (
    c1 NVARCHAR(15), c2 NVARCHAR(255), c3 NVARCHAR(255), c4 NVARCHAR(255),
    c5 NVARCHAR(50), c6 NVARCHAR(50), c7 NVARCHAR(50), c8 NVARCHAR(50),
    c9 NVARCHAR(30), c10 NVARCHAR(30), c11 NVARCHAR(30)
);
GO

BULK INSERT dbo.PadronRncStaging
FROM 'C:\Temp\DGII_RNC.TXT'
WITH (FIELDTERMINATOR = '|', ROWTERMINATOR = '0x0d0a', CODEPAGE = '1252', TABLOCK);
GO

IF OBJECT_ID('dbo.PadronRnc') IS NOT NULL DROP TABLE dbo.PadronRnc;
CREATE TABLE dbo.PadronRnc (
    Rnc    NVARCHAR(11) NOT NULL PRIMARY KEY,   -- RNC/Cédula con ceros a la izquierda
    Nombre NVARCHAR(200) NOT NULL,
    Estado NVARCHAR(20) NULL
);
GO

-- Dedupe por RNC (el padrón puede traer duplicados); nos quedamos con uno
INSERT INTO dbo.PadronRnc (Rnc, Nombre, Estado)
SELECT RIGHT('00000000000' + LTRIM(RTRIM(c1)), 11) AS Rnc,
       MAX(LTRIM(RTRIM(c2))) AS Nombre,
       MAX(LTRIM(RTRIM(c10))) AS Estado
FROM dbo.PadronRncStaging
WHERE c1 IS NOT NULL AND LTRIM(RTRIM(c1)) <> '' AND LTRIM(RTRIM(c2)) <> ''
GROUP BY RIGHT('00000000000' + LTRIM(RTRIM(c1)), 11);
GO

DROP TABLE dbo.PadronRncStaging;

-- gc_app (login de la app) solo necesita leer; db_datareader ya lo cubre.
SELECT COUNT(*) AS RegistrosPadron FROM dbo.PadronRnc;
GO
