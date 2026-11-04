# Shinrikai — port para .NET 8 + React

Port do Shinrikai (ASP.NET MVC 5 / .NET Framework 4.5) seguindo o plano em `SHINRIKAI_PORT.md`.
O código legado continua intacto na raiz do repositório como referência.

## Estrutura

```
port/
├── Shinrikai.sln
├── Nienow.Helper/               Monitor, ShuffleList, CleanText
├── Nienow.Shinrikai.Core/       Enums, Response e Adaptive/ (os 4 algoritmos como funções puras)
├── Nienow.DataBaseConn/         Conexão Firebird + helpers ADO.NET 100% parametrizados
├── Nienow.Shinrikai.User/       Usuários, login, papéis (A/T/S)
├── Nienow.Shinrikai.Vocabulary/ Módulo piloto: repositório + serviço sem estado
├── Nienow.Shinrikai.Monitoring/ Log de interações (INTERACTIONS)
├── Nienow.Shinrikai/            API ASP.NET Core 8 (JWT, Swagger, serve o SPA)
├── tests/Nienow.Shinrikai.Tests xUnit — inclui oráculo com cópia literal do código original
└── web/                         React + Vite + TipTap
```

## Status

| Fase | Situação |
|---|---|
| 1. Solution .NET 8, `Program.cs`, `appsettings.json` | ✅ |
| 2. Firebird + SQL parametrizado | ✅ para User, Vocabulary, Monitoring · ⏳ Grammar, Kanji, Text, Exercises, Dictionary |
| 3. JWT | ✅ |
| 4. Front React | ✅ login + vocabulário · ⏳ demais módulos |
| 5. Editor TipTap | ✅ protótipo (`/editor`): furigana + link de vocabulário |
| 6. Deploy | ⏳ |

Os 4 algoritmos de adaptabilidade (seção 4 do documento) já estão portados em
`Nienow.Shinrikai.Core/Adaptive` e testados 1:1 contra o original, mesmo para módulos cuja
API ainda não foi portada.

## Rodando

```bash
# testes
dotnet test port/Shinrikai.sln

# API (http://localhost:5080, Swagger em /swagger)
dotnet user-secrets --project port/Nienow.Shinrikai set "ConnectionStrings:ShinrikaiDB" "character set=UTF8;initial catalog=C:\caminho\SHINRIKAI.FDB;data source=localhost;port=3050;dialect=3;user id=SYSDBA;password=..."
dotnet run --project port/Nienow.Shinrikai

# front (http://localhost:5173, /api é encaminhado para a 5080)
npm --prefix port/web install
npm --prefix port/web run dev
```

Em produção, `Jwt:Key` (≥ 32 bytes) e a connection string vêm de variáveis de ambiente
(`Jwt__Key`, `ConnectionStrings__ShinrikaiDB`). `npm run build` publica o SPA em
`Nienow.Shinrikai/wwwroot`, servido pela própria API.

### Banco: conversão de ODS 11 → 13

O `SHINRIKAI.FDB` existente é **ODS 11.2 (Firebird 2.5)**. O Firebird 4 recusa abrir
(`unsupported on-disk structure … found 11.2, support 13.0`). É preciso um backup com o
`gbak` do 2.5 e restore com o do 4:

```bash
# com Firebird 2.5 (ex.: kit zip/embedded)
gbak -b -user SYSDBA -password masterkey SHINRIKAI.FDB shinrikai.fbk
# com Firebird 4
gbak -c -user SYSDBA -password <senha> shinrikai.fbk SHINRIKAI_FB4.FDB
```

## Decisões e diferenças em relação ao original

- **Sem estado de sessão.** O `VocabularyManager` vivia na `Session` do MVC. Agora
  `POST /api/vocabulary/sessions` devolve todas as páginas (10 itens, embaralhadas) e o cliente
  navega; `POST /api/vocabulary/check` corrige por `WordId` no servidor e aplica o contador.
- **Variação de fragmento — não é idêntica entre módulos.** Vocabulário e Kanji contam sucesso
  quando `Counter <= 1`; Transcrição só quando `Counter <= 0`; o clique no stretchtext conta
  falha só se `Counter == 0`. O port mantém cada variante (`FragmentVariation.Apply*`).
- **`HasEnoughLevel`** foi reduzido a `content <= user + (restrict ? 0 : 1)`; a equivalência
  com o `switch` original é verificada em todas as 72 combinações.
- **Login** comparava a senha dentro do SQL concatenado (SQL injection). Agora a senha é lida por
  login parametrizado e comparada em tempo constante. ⚠️ A base guarda senhas **em texto puro**;
  migrar para hash exige aumentar `USERS.USR_PASS` (pendente).
- **Cadastro de usuário:** o original só checava se "repetir senha" estava vazio; agora compara.
- **Cadastro de vocabulário:** a validação das possibilidades 2 e 3 tinha condições contraditórias;
  agora, se qualquer campo da possibilidade é preenchido, assunto e tradução são obrigatórios.
- **Editor:** furigana e palavra de vocabulário são *nodes* TipTap (não *marks*), porque `<ruby>`
  tem dois textos (base e leitura). O HTML gerado é exatamente o que a regex do backend
  (`Stretchtext.ApplyKnowledge`) reconhece. A notação antiga `漢字「かんじ」` é convertida ao abrir.
