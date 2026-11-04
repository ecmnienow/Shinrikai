# Shinrikai

## Visão geral do código

Sistema hipermídia adaptativa para estudo de japonês (TCC de Éverton C. M. Nienow, UCS), portado de
ASP.NET MVC 5 / .NET Framework 4.5 / IIS para .NET 10 + React. O núcleo acadêmico são as 4 técnicas de
adaptatividade em `Nienow.Shinrikai.Core/Adaptive`, testadas 1:1 contra cópias literais do código original
em `tests/Nienow.Shinrikai.Tests/Legacy` (não refatore essas cópias).

**Stack:**
- Backend: ASP.NET Core em Kestrel (API `/api`, JWT, Swagger), ADO.NET parametrizado sobre Firebird 4,
  xUnit.
- Front: React 18 + Vite + TipTap 2.27.3.

**Estrutura:**
- Bibliotecas base: `Nienow.Helper`, `Nienow.Shinrikai.Core`, `Nienow.DataBaseConn`.
- Um projeto por domínio (`Nienow.Shinrikai.{User,Vocabulary,Kanji,Grammar,Text,Exercises,Dictionary,Monitoring}`),
  cada um com repositório + serviço.
- API em `Nienow.Shinrikai/`, front em `web/`, scripts do banco em `database/`, testes em `tests/`.

**Convenções:**
- `ImplicitUsings` desligado: usings explícitos, `System.*` primeiro.
- Namespaces sempre em bloco (`namespace X { }`).
- SQL sempre parametrizado.
- Comentários e UI em português.

**Comandos:**
- Testes: `dotnet test Nienow.Shinrikai.sln`.
- API: `dotnet run --project Nienow.Shinrikai --launch-profile Shinrikai` (porta 5080).
- Front: `npm --prefix web run dev` (porta 5173).
- Banco local: `database/create-database.ps1`.

Para a arquitetura detalhada, consulte [docs/CODEBASE_MAP.md](docs/CODEBASE_MAP.md).
