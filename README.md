# Shinrikai — .NET 10 + React

Sistema hipermídia adaptativa para o estudo da língua japonesa (TCC — UCS), portado de
ASP.NET MVC 5 / .NET Framework 4.5 + IIS para .NET 10 + Kestrel + React, seguindo o plano em
`SHINRIKAI_PORT.md`.

O código legado (MVC 5) foi removido deste branch. Ele continua no `master` e no histórico do
git (ex.: `git show 715ce03:Nienow.Shinrikai.Kanji/KanjiManager.cs`). O comportamento dos
algoritmos foi preservado em `tests/Nienow.Shinrikai.Tests/Legacy/` (cópias literais usadas como
oráculo nos testes).

## Estrutura

```
.
├── Nienow.Shinrikai.sln
├── Nienow.Helper/               Monitor, ShuffleList, CleanText
├── Nienow.Shinrikai.Core/       Enums, Response e Adaptive/ (os 4 algoritmos como funções puras)
├── Nienow.DataBaseConn/         Firebird + helpers ADO.NET 100% parametrizados; MonitorStore/KnowledgeStore
├── Nienow.Shinrikai.User/       Usuários, login, cadastro, papéis (A/T/S), notificações de ajuda
├── Nienow.Shinrikai.Vocabulary/ Vocabulário + Transcrição
├── Nienow.Shinrikai.Kanji/      Leitura de kanjis + cadastro
├── Nienow.Shinrikai.Grammar/    Mapa adaptativo de gramática + cadastro
├── Nienow.Shinrikai.Text/       Leitura com stretchtext + cadastro
├── Nienow.Shinrikai.Exercises/  Exercícios (ordenação adaptativa) + cadastro
├── Nienow.Shinrikai.Dictionary/ Dicionário
├── Nienow.Shinrikai.Monitoring/ Log de interações (INTERACTIONS)
├── Nienow.Shinrikai/            API ASP.NET Core 10 em Kestrel (JWT, Swagger, serve o SPA)
├── tests/Nienow.Shinrikai.Tests xUnit — inclui oráculo com cópia literal do código original
└── web/                         React + Vite + TipTap
```

## Paridade com o sistema original

Todas as telas, ações e regras do MVC 5 têm equivalente:

| Original (MVC 5) | Novo (rota React → API) |
|---|---|
| Home/Index | `/` |
| Home/About | `/sobre` |
| Login/Index (login) · Login/Logout | `/login` → `POST /api/auth/login` · `POST /api/auth/logout` |
| Login/Index (cadastro) · Login/SignUp | `/login` → "Cadastre-se" → `POST /api/auth/signup` |
| Home/GetNotification · DeclareUnderstanding (Ajuda 1, 3–7) | botão **?** em cada tela → `/api/notifications/{id}` |
| Transcription/Index (+ Generate/Next/Previous/Reload/Check/ShowPageStatus) | `/transcricao` → `/api/transcription/*` |
| Transcription/SearchKanjiWord | `GET /api/transcription/kanji-words` (usado no editor de textos) |
| Vocabulary/Index (+ ações de estudo) | `/vocabulario` → `/api/vocabulary/*` |
| Vocabulary/Register · AddWord · GetWord | `/professor/vocabulario` → `/api/vocabulary/words` |
| Kanjis/Index (+ GenerateLists/Next/Previous/ReloadList/Check) | `/kanjis` → `/api/kanjis/*` |
| Kanjis/Register · AddKanji · UpdateKanji · GetKanji | `/professor/kanjis` → `/api/kanjis` |
| Grammar/Index · GetGrammar · GetRequisites · DeclareKnowledge | `/gramatica` → `/api/grammar/*` (timer de 20 s) |
| Grammar/Register · AddGrammar · ListAllGrammars | `/professor/gramatica` → `/api/grammar`, `/api/grammar/all` |
| Text/Index · GetText · DeclareKnowledge · RegisterLackOfKnowledge | `/textos` → `/api/texts/*` (stretchtext, timer de 20 s) |
| Text/Register · AddText | `/professor/textos` → `/api/texts` |
| Exercises/Index · GetExercise · CheckAnswer | `/exercicios?grammarId=&textId=` → `/api/exercises/*` |
| Exercises/Register · AddExercise | `/professor/exercicios?id=&grammarId=&textId=` → `/api/exercises` |
| Dictionary/Index · Search · GetImage · GetAudio | `/dicionario` → `/api/dictionary/search`, mídia em `/api/vocabulary/words/{id}/{image\|audio}/{slot}` |
| Registro em INTERACTIONS (todas as telas) | mantido, com as mesmas telas/ações |
| `bootstrap.dialogs` / `bootstrap.image` | `components/Dialogs.tsx` (alerta, erro, info, sucesso, pergunta, imagem) |
| Logo (`Content/Images/Logo.png`) | `web/public/logo.png` (também favicon) |

Não foram portados: o menu lateral do `_Layout` (template de exemplo com "Jhon Smith", "E-commerce",
"Dashboard" — sem função no sistema) e o menu "Administrador" que já estava comentado no original.

## Rodando

```bash
# testes
dotnet test Nienow.Shinrikai.sln

# API (http://localhost:5080, Swagger em /swagger)
dotnet user-secrets --project Nienow.Shinrikai set "ConnectionStrings:ShinrikaiDB" "character set=UTF8;initial catalog=C:\caminho\SHINRIKAI.FDB;data source=localhost;port=3050;dialect=3;user id=SYSDBA;password=..."
dotnet run --project Nienow.Shinrikai

# front (http://localhost:5173, /api é encaminhado para a 5080)
npm --prefix web install
npm --prefix web run dev
```

Em produção, `Jwt:Key` (≥ 32 bytes) e a connection string vêm de variáveis de ambiente
(`Jwt__Key`, `ConnectionStrings__ShinrikaiDB`).

### Hospedagem (Kestrel)

A API roda direto no **Kestrel**, sem IIS (o `Web.config` não existe mais). Os endpoints ficam na
seção `Kestrel` do `appsettings.json` e podem ser sobrescritos no ambiente:

```bash
# porta/host
Kestrel__Endpoints__Http__Url=http://0.0.0.0:8080
# HTTPS direto no Kestrel
Kestrel__Endpoints__Https__Url=https://0.0.0.0:8443
Kestrel__Endpoints__Https__Certificate__Path=/caminho/cert.pfx
Kestrel__Endpoints__Https__Certificate__Password=...
```

Atrás de um proxy reverso (nginx, Caddy, IIS como proxy), defina `ReverseProxy__Enabled=true`
para respeitar `X-Forwarded-For`/`X-Forwarded-Proto`. Publicação:
`dotnet publish Nienow.Shinrikai -c Release -o out` e então `dotnet out/Nienow.Shinrikai.dll`.
`npm --prefix web run build` publica o SPA em `Nienow.Shinrikai/wwwroot`, servido pela própria API.

### Banco novo (vazio)

`database/` cria o banco no Firebird local com o mesmo esquema do original (16 tabelas, 15
generators), os textos de ajuda e uma lista inicial de assuntos, e grava a connection string no
perfil `Shinrikai` do `launchSettings.json` (que o Visual Studio e o `dotnet run` aplicam ao processo)
e em user-secrets — sem alterar código. Para apontar para outro banco depois:
`.\database\configure-connection.ps1 -Path <arquivo.fdb>`.

```powershell
.\database\create-database.ps1                       # cria database\data\SHINRIKAI.FDB (ignorado pelo git)
# reinicie a API, cadastre-se pela tela de login e então:
.\database\promote-user.ps1 -Login <seu-login>       # vira administrador (menu Professor)
```

Parâmetros: `-Path`, `-User`, `-Password`, `-Server`, `-Port`, `-Isql`, `-SkipUserSecrets`.
O cadastro pela tela sempre cria alunos e não há tela de papéis nem de assuntos (os menus
"Controle de usuários" e "Cadastro de assuntos" estavam desativados no original) — para isso são
`promote-user.ps1` e o `INSERT` comentado em `database/seed.sql`.

### Servidor Firebird

O projeto roda no **Firebird 5.0.4** (também funciona no 4.0), como serviço na porta 3050. O
`firebird.conf` usa as mesmas configurações do servidor anterior, que outros sistemas da máquina
esperam:

```
AuthServer = Srp256, Srp, Legacy_Auth
UserManager = Srp, Legacy_UserManager
WireCrypt = Enabled
ServerMode = Super
```

O Firebird 5 abre bancos do Firebird 4 (ODS 13.0) sem convertê-los, então eles continuam abrindo no 4.
Para levar um banco ao formato do 5 (ODS 13.1), faça backup e restore com o `gbak` do 5. Os scripts de
`database/` usam o `isql` da versão mais nova instalada em `C:\Program Files\Firebird`.

### Banco antigo: conversão de ODS 11 → 13.1

Para usar os dados do sistema original em vez do banco novo: o `SHINRIKAI.FDB` existente é
**ODS 11.2 (Firebird 2.5)**. Os Firebird 4 e 5 recusam abrir
(`unsupported on-disk structure … found 11.2`). É preciso um backup com o `gbak` do 2.5 e restore
com o do servidor atual.

Sem o Firebird 2.5 instalado, use o kit zip oficial
([`Firebird-2.5.9.27139-0_x64.zip`](https://github.com/FirebirdSQL/firebird/releases/tag/R2_5_9), que
não tem `fbembed.dll`) rodando o servidor como aplicação numa porta própria, sem conflitar com o
servidor da porta 3050. Acrescente ao `firebird.conf` do kit:

```
RemoteServicePort = 3051
IpcName = FIREBIRD25
RemotePipeName = interbas25
GuardianOption = 0
```

```bash
# Firebird 2.5 (kit zip): servidor temporário e backup de uma CÓPIA do banco
bin\fbserver.exe -a
bin\gbak.exe -b -g -v -user SYSDBA -password masterkey localhost/3051:C:\copia\SHINRIKAI.FDB C:\copia\shinrikai.fbk
# Firebird 5 (encerre o fbserver do 2.5 antes)
"C:\Program Files\Firebird\Firebird_5_0\gbak.exe" -c -v -user SYSDBA -password <senha> C:\copia\shinrikai.fbk localhost/3050:D:\Projetos\Shinrikai\database\data\SHINRIKAI_LEGADO.FDB
```

Depois do restore, **iguale os generators aos maiores IDs**. O `DBText` original gravava em
`TRL_MONITOR` com o `VOCMONITOR_ID_GEN`, então o `TRLCMONITOR_ID_GEN` fica atrás do maior `TMN_ID`
(no banco de 2020: 89 contra 187) e a próxima gravação de transcrição colidiria com um ID existente.
O banco original também não tem o `SUBJECTS_ID_GEN` usado pelo `seed.sql`:

```sql
SELECT GEN_ID(TRLCMONITOR_ID_GEN, 0), (SELECT MAX(TMN_ID) FROM TRL_MONITOR) FROM RDB$DATABASE;
SET GENERATOR TRLCMONITOR_ID_GEN TO 187;   -- o MAX(TMN_ID) encontrado
CREATE SEQUENCE SUBJECTS_ID_GEN;
COMMIT;
SET GENERATOR SUBJECTS_ID_GEN TO 37;       -- o MAX(SBJ_ID) encontrado
COMMIT;
```

Por fim, aponte a API para o banco convertido:
`.\database\configure-connection.ps1 -Path D:\Projetos\Shinrikai\database\data\SHINRIKAI_LEGADO.FDB`.

O esquema do banco não muda: o port usa as mesmas tabelas, colunas e generators do original. O banco
original já é UTF8, e o texto em japonês passa intacto pelo backup/restore.

## Editor (cadastros de Gramática e Texto)

Substitui o CKEditor 4 por TipTap sem perder conteúdo:

- Formatação equivalente ao CKEditor padrão: negrito, itálico, sublinhado, tachado, sub/sobrescrito,
  cor, marca-texto, títulos, listas, citação, alinhamento, links, imagens, tabelas, linha horizontal.
- **Modo HTML** (o "Código-fonte" do CKEditor): edita o HTML cru, sem normalização.
- **Verificação de fidelidade:** ao abrir um conteúdo existente, compara tags, atributos e texto do
  HTML original com o que o editor visual produziria. Se algo fosse descartado (ex.: `<font>`,
  `style` desconhecido), abre direto em modo HTML com aviso — o conteúdo salvo fica idêntico ao do
  banco, a menos que seja editado.
- **字 Furigana** e **🔗 Vocabulário** (só em Textos): geram `<ruby>` e
  `<span class="stretchText" data-id="N">`, exatamente o que o backend reconhece. Antes era
  notação manual `漢字「かんじ」` e HTML cru; a notação antiga é convertida ao abrir.

## Decisões e diferenças em relação ao original

Regras preservadas 1:1 (testadas contra cópias literais do código original):

- **Variação de fragmento — não é idêntica entre módulos.** Vocabulário e Kanji contam sucesso
  quando `Counter <= 1`; Transcrição só quando `Counter <= 0`; o clique no stretchtext conta
  falha só se `Counter == 0`; exercícios só acumulam acertos/erros.
- **`HasEnoughLevel`** reduzido a `content <= user + (restrict ? 0 : 1)`; equivalência com o
  `switch` original verificada nas 72 combinações.
- **Kanji:** `CheckReading` (soma 1/2/4 por posição; respostas repetidas não contam) e a regra
  "acerta só com todas as leituras" — verificados em 1029 combinações.
- **Dicionário:** mesma ordem dos resultados (exato → começa com → contém), sensível a maiúsculas.

Correções de bugs do original (o comportamento pretendido foi mantido):

- **Sem estado de sessão.** Os *Managers* viviam na `Session` do MVC. Agora a API devolve todas as
  páginas (10 itens, embaralhadas) e corrige por ID, no servidor.
- **SQL injection** em praticamente todas as consultas (inclusive o login). Tudo parametrizado.
- **Senhas em texto puro** na base. ⚠️ Mantido por compatibilidade; migrar para hash exige
  aumentar `USERS.USR_PASS` (pendente).
- **Cadastro de usuário:** "repetir senha" agora é comparada e a senha mínima de 5 dígitos (que só
  existia no navegador) também vale no servidor.
- **Cadastro de vocabulário:** a validação das formas 2 e 3 tinha condições contraditórias; agora,
  se qualquer campo da forma é preenchido, assunto e tradução são obrigatórios.
- **Kanji:** o cadastro gravava o nível em minúsculas ("n5") e o estudo filtrava por "N5" — kanjis
  cadastrados pela tela não apareciam no estudo. Agora o nível é normalizado e comparado sem caixa.
  O original também recusava kanjis fora do BMP (`Length == 1`).
- **Transcrição:** palavras sem kanji no assunto eram puladas só na exibição, desalinhando a
  correção (feita por posição). Agora ficam fora da lista e a correção é por ID.
- **Exercícios:** `GetExercise` enviava a resposta certa ao navegador; agora ela fica no servidor.
  A mensagem de erro citava o número da opção (que aparece embaralhada); agora cita o texto.
- **Dicionário:** só servia a imagem/áudio da forma 1; agora usa a primeira forma que tiver mídia.
- **Stretchtext:** o `DBText` inseria em `TRL_MONITOR` com o generator de outra tabela
  (`VOCMONITOR_ID_GEN`); agora usa o da própria tabela (`TRLCMONITOR_ID_GEN`).
- **INTERACTIONS:** a abertura de exercícios de texto registrava o ID da gramática; corrigido.
