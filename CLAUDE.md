# Shinrikai

## Visão geral do código

Sistema hipermídia adaptativa para estudo de japonês (TCC de Éverton C. M. Nienow, UCS), portado de
ASP.NET MVC 5 / .NET Framework 4.5 / IIS para .NET 10 + React. O núcleo acadêmico são as 4 técnicas de
adaptatividade em `Nienow.Shinrikai.Core/Adaptive`, testadas 1:1 contra cópias literais do código original
em `tests/Nienow.Shinrikai.Tests/Legacy` (não refatore essas cópias).

**Stack:**
- Backend: ASP.NET Core em Kestrel (API `/api`, JWT, Swagger), ADO.NET parametrizado sobre Firebird 5.0.4,
  xUnit.
- Front: React 18 + Vite + TipTap 2.27.3.

**Estrutura:**
- Bibliotecas base: `Nienow.Helper`, `Nienow.Shinrikai.Core`, `Nienow.DataBaseConn`.
- Um projeto por domínio (`Nienow.Shinrikai.{User,Vocabulary,Kanji,Grammar,Text,Exercises,Dictionary,Monitoring}`),
  cada um com repositório + serviço.
- API em `Nienow.Shinrikai.Api/` (só `/api`).
- Interface em `Nienow.Shinrikai.Web/`: host Kestrel que serve o build do React e encaminha `/api` para a
  API via YARP; o fonte React fica em `Nienow.Shinrikai.Web/ClientApp/`.
- Scripts do banco em `database/`, testes em `tests/`.

**Convenções:**
- `ImplicitUsings` desligado: usings explícitos, `System.*` primeiro.
- Namespaces sempre em bloco (`namespace X { }`).
- SQL sempre parametrizado.
- Comentários e UI em português.

**Comandos:**
- Testes: `dotnet test Nienow.Shinrikai.sln`.
- API: `dotnet run --project Nienow.Shinrikai.Api --launch-profile Shinrikai` (porta 5080).
- Front em dev: `npm --prefix Nienow.Shinrikai.Web/ClientApp run dev` (porta 5173, Vite encaminha `/api`).
- Interface como em produção: `npm --prefix Nienow.Shinrikai.Web/ClientApp run build` e
  `dotnet run --project Nienow.Shinrikai.Web` (porta 5090).
- Banco local: `database/create-database.ps1`.

Para a arquitetura detalhada, consulte [docs/CODEBASE_MAP.md](docs/CODEBASE_MAP.md).
