# Importação dos kanjis N5 (joyokanjikai.com)

Fonte: <https://www.joyokanjikai.com/learning-japanese/complete-list-of-all-jlpt-n5-kanji-with-readings-meanings-examples/> (103 kanjis, 2 exemplos cada; 194 palavras sem repetição).
Banco comparado: `database/data/SHINRIKAI_LEGADO.FDB` (357 kanjis, 1382 palavras, 37 assuntos).

**Aplicado em 2026-11-22** (itens 1 a 5 e as correções do item 7; o item 8 só no local e no Docker) no banco do Azure (VM `vm-shinrikai-db`), no banco local `SHINRIKAI_LEGADO.FDB` e no banco do Docker (`shinrikai.fdb`, volume `shinrikai_firebird-data`), pelo script [database/import/kanjis-n5-joyokanjikai.sql](../database/import/kanjis-n5-joyokanjikai.sql). Resultado: 1425 palavras, 39 assuntos, 103 kanjis N5. Backups de antes (gbak) em `database/data/backup-2026-11-22-antes-n5/` (local e Docker) e em `/srv/firebird/backup-2026-11-22-antes-n5.fbk` na VM do Azure.

Convenções seguidas (as do cadastro do professor): um registro por kana, com homônimos e outros sentidos como forma 2/3; kana em hiragana (katakana só em estrangeirismo); romaji minúsculo com `ou`/`uu`; tradução em português, alternativas separadas por vírgula **sem espaço**; nível JLPT gravado como `N5`.

## 1. Assuntos novos (2)

| Assunto | Por quê | Itens |
|---|---|---|
| direções | norte/sul/leste/oeste estão em *natureza* e esquerda/direita em *outros*; o assunto junta tudo sem tirar nada de onde o professor pôs (entra como forma 2) | 8 |
| conceitos | substantivos abstratos que hoje só caberiam em *outros* | 6 |

## 2. Leitura de kanjis: 28 kanjis passam a ser N5

Os 103 kanjis da página **já estão cadastrados**. 75 já são N5. Dos 28 abaixo, 23 estão sem nível (só aparecem em N1) e 4 em N4; com `N5` eles entram na sessão N5 (e continuam nas de N4 a N1). O 車 está como `n5` minúsculo, que a sessão já conta como N5: só padroniza. A sessão N5 passa de 76 para 103 kanjis.

| Kanji | Leituras no banco | Nível atual |
|---|---|---|
| 北 | きた,ほく | sem nível |
| 南 | みなみ,なん | sem nível |
| 花 | はな,か | sem nível |
| 魚 | さかな,うお,ぎょ | sem nível |
| 空 | そら,から,あ,くう | sem nível |
| 目 | め,ま,もく,ぼく | sem nível |
| 口 | くち,く,こう | sem nível |
| 耳 | みみ,じ | sem nível |
| 手 | て,た,しゃ | sem nível |
| 足 | あし,た,そく | sem nível |
| 言 | い,こと | sem nível |
| 飲 | の | N4 |
| 会 | あ,かい,え | sem nível |
| 買 | か,ばい | sem nível |
| 立 | た,りつ,りゅう | sem nível |
| 古 | ふる,こ | sem nível |
| 高 | たか,こう | sem nível |
| 安 | やす,あん | N4 |
| 多 | おお,た | sem nível |
| 新 | あたら,にい,しん | sem nível |
| 少 | すく,すこ,しょう | sem nível |
| 長 | なが,ちょう | sem nível |
| 週 | しゅう | N4 |
| 店 | みせ,てん | sem nível |
| 道 | みち,どう | sem nível |
| 車 | くるま,しゃ | n5 |
| 駅 | えき | N4 |
| 社 | やしろ,しゃ | sem nível |

## 3. Palavras novas (44)

Entram no **Estudo de vocabulário** e, como têm kanji, na **Transcrição para hiragana**.

| Assunto | Kanji | Kana | Romaji | Tradução |
|---|---|---|---|---|
| contagem | 百円 | ひゃくえん | hyakuen | cem ienes |
| contagem | 百人 | ひゃくにん | hyakunin | cem pessoas |
| contagem | 千円 | せんえん | senen | mil ienes |
| tempo | 一月 | いちがつ | ichigatsu | janeiro |
| tempo | 何時 | なんじ | nanji | que horas |
| tempo | 休日 | きゅうじつ | kyuujitsu | feriado,dia de folga |
| tempo | 来年 | らいねん | rainen | ano que vem,próximo ano |
| tempo | 古代 | こだい | kodai | antiguidade,tempos antigos |
| tempo | 分 | ふん | fun | minuto,minutos |
| tempo | 先週 | せんしゅう | senshuu | semana passada |
| tempo | 午前 | ごぜん | gozen | manhã |
| tempo | 半 | はん | han | meia,e meia |
| lugares | 千葉 | ちば | chiba | Chiba |
| lugares | 北アメリカ | きたアメリカ | kitaamerika | América do Norte |
| lugares | 東京 | とうきょう | toukyou | Tóquio |
| lugares | 西洋 | せいよう | seiyou | Ocidente |
| lugares | 南アメリカ | みなみアメリカ | minamiamerika | América do Sul |
| países | 日本国 | にほんこく | nihonkoku | Japão,Estado do Japão |
| direções | 左側 | ひだりがわ | hidarigawa | lado esquerdo |
| direções | 右側 | みぎがわ | migigawa | lado direito |
| natureza | 河川 | かせん | kasen | rio,rios |
| natureza | 雨天 | うてん | uten | tempo chuvoso |
| natureza | 金 | きん | kin | ouro |
| pessoas | 日本人 | にほんじん | nihonjin | japonês,japonesa |
| pessoas | 女性 | じょせい | josei | mulher,sexo feminino |
| pessoas | 男性 | だんせい | dansei | homem,sexo masculino |
| pessoas | 子 | こ | ko | criança,filho,filha |
| pessoas | 友人 | ゆうじん | yuujin | amigo |
| escola | 小学校 | しょうがっこう | shougakkou | escola primária,ensino fundamental |
| escritório | 万年筆 | まんねんひつ | mannenhitsu | caneta-tinteiro,caneta tinteiro |
| estação | 駅前 | えきまえ | ekimae | em frente à estação |
| saúde | 耳鼻科 | じびか | jibika | otorrinolaringologia,otorrino |
| verbos | 足りる | たりる | tariru | bastar,ser suficiente |
| verbos | 下げる | さげる | sageru | abaixar,baixar |
| adjetivos | 安全 | あんぜん | anzen | seguro |
| adjetivos | 円い | まるい | marui | redondo |
| advérbios | 中 | なか | naka | dentro,meio |
| advérbios | 前 | まえ | mae | frente,antes |
| cores | 白 | しろ | shiro | branco |
| conceitos | 目的 | もくてき | mokuteki | objetivo,propósito,finalidade |
| conceitos | 人口 | じんこう | jinkou | população |
| conceitos | 生 | せい | sei | vida |
| conceitos | 多数 | たすう | tasuu | maioria |
| conceitos | 社会 | しゃかい | shakai | sociedade |

## 4. Palavras existentes que ganham uma forma nova (11)

O kana já existe com outro sentido ou em outro assunto. Pela regra de um registro por kana, o sentido novo entra como forma 2, sem mexer na forma 1.

| ID | Kana | Forma(s) atual(is) | Nova forma: kanji · assunto · tradução |
|---|---|---|---|
| 72 | ひだり | 左 · outros · esquerda | 左 · direções · esquerda |
| 73 | みぎ | 右 · outros · direita | 右 · direções · direita |
| 83 | き | 木 · natureza · árvore | 気 · conceitos · espírito,ânimo,energia |
| 231 | じ | 字 · escola · letra | 時 · tempo · hora,horas |
| 840 | した | — · partes do corpo · língua | 下 · advérbios · embaixo,abaixo |
| 1168 | きた | — · natureza · norte | 北 · direções · norte |
| 1169 | みなみ | — · natureza · sul | 南 · direções · sul |
| 1170 | にし | — · natureza · oeste | 西 · direções · oeste |
| 1171 | ひがし | — · natureza · leste | 東 · direções · leste |
| 1248 | あと | — · adjetivos · último | 後 · tempo · depois,após |
| 1371 | しゅうかん | — · outros · hábito,costume | 週間 · tempo · semana,semanas |

## 5. Palavras existentes que ganham o kanji (99 registros, 100 formas)

Estão cadastradas só em kana. Com o kanji preenchido na forma do mesmo sentido, passam a aparecer na **Transcrição para hiragana**. Assunto e tradução não mudam, exceto onde indicado.

| ID | Kana | Forma | Kanji | Assunto | Tradução |
|---|---|---|---|---|---|
| 488 | ひとり | 1 | 一人 | contagem | um |
| 536 | ひとつ | 1 | 一つ | contagem | um |
| 489 | ふたり | 1 | 二人 | contagem | dois |
| 537 | ふたつ | 1 | 二つ | contagem | dois |
| 513 | みっか | 1 | 三日 | tempo | dia 3 |
| 538 | みっつ | 1 | 三つ | contagem | três |
| 514 | よっか | 1 | 四日 | tempo | dia 4 |
| 539 | よっつ | 1 | 四つ | contagem | quatro |
| 515 | いつか | 1 | 五日 | tempo | dia 5 |
| 540 | いつつ | 1 | 五つ | contagem | cinco |
| 516 | むいか | 1 | 六日 | tempo | dia 6 |
| 541 | むっつ | 1 | 六つ | contagem | seis |
| 517 | なのか | 1 | 七日 | tempo | dia 7 |
| 542 | ななつ | 1 | 七つ | contagem | sete |
| 518 | ようか | 1 | 八日 | tempo | dia 8 |
| 543 | やっつ | 1 | 八つ | contagem | oito |
| 519 | ここのか | 1 | 九日 | tempo | dia 9 |
| 544 | ここのつ | 1 | 九つ | contagem | nove |
| 520 | とおか | 1 | 十日 | tempo | dia 10 |
| 469 | じゅう | 1 | 十 | contagem | dez |
| 487 | いちまん | 1 | 一万 | contagem | dez mil |
| 622 | すいようび | 1 | 水曜日 | tempo | quarta-feira |
| 423 | みず | 1 | 水 | natureza | água |
| 621 | かようび | 1 | 火曜日 | tempo | terça-feira |
| 787 | はなび | 1 | 花火 | outros | fogo de artifício |
| 623 | もくようび | 1 | 木曜日 | tempo | quinta-feira |
| 396 | てんき | 1 | 天気 | natureza | tempo,clima |
| 792 | てん | 1 | 天 | natureza | céu |
| 625 | どようび | 1 | 土曜日 | tempo | sábado |
| 640 | つち | 1 | 土 | natureza | terra,solo |
| 1168 | きた | 1 | 北 | natureza | norte |
| 1171 | ひがし | 1 | 東 | natureza | leste |
| 1170 | にし | 1 | 西 | natureza | oeste |
| 1169 | みなみ | 1 | 南 | natureza | sul |
| 626 | にちようび | 1 | 日曜日 | tempo | domingo |
| 586 | きょう | 1 | 今日 | tempo | hoje |
| 620 | げつようび | 1 | 月曜日 | tempo | segunda-feira |
| 364 | はな | 1 | 花 | natureza | flor |
| 345 | はなみ | 1 | 花見 | natureza | piquenique |
| 1154 | きんぎょ | 1 | 金魚 | animais | peixe dourado |
| 791 | そら | 1 | 空 | natureza | céu |
| 596 | くうこう | 1 | 空港 | lugares | aeroporto |
| 452 | やま | 1 | 山 | natureza | montanha |
| 404 | ふじさん | 1 | 富士山 | lugares | Monte Fuji |
| 505 | あめ | 1 | 雨 | natureza | chuva |
| 683 | め | 1 | 目 | partes do corpo | olho |
| 685 | くち | 1 | 口 | partes do corpo | boca |
| 684 | みみ | 1 | 耳 | partes do corpo | orelha |
| 351 | てがみ | 1 | 手紙 | escritório | carta |
| 686 | あし | 1 | 足 | partes do corpo | pé |
| 686 | あし | 2 | 足 | partes do corpo | perna |
| 439 | はは | 1 | 母 | pessoas | mãe |
| 1277 | ははおや | 1 | 母親 | pessoas | mãe |
| 1276 | ちち | 1 | 父 | pessoas | pai |
| 1278 | ちちおや | 1 | 父親 | pessoas | pai |
| 704 | おんな | 1 | 女 | pessoas | mulher |
| 705 | おとこ | 1 | 男 | pessoas | homem |
| 89 | こども | 1 | 子供 | pessoas | criança |
| 369 | ちいさい | 1 | 小さい | adjetivos | pequeno |
| 98 | おとな | 1 | 大人 | pessoas | adulto |
| 503 | あげる | 1 | 上げる | verbos | ~~dar~~ → **dar,levantar,erguer** |
| 323 | なに | 1 | 何 | pronomes | o que,qual |
| 457 | ぎんこう | 1 | 銀行 | lugares | banco |
| 1114 | けんがく | 1 | 見学 | escola | visita de estudo |
| 773 | いう | 1 | 言う | verbos | dizer |
| 354 | ことば | 1 | 言葉 | escola | palavra |
| 638 | しょくじ | 1 | 食事 | outros | refeição |
| 401 | のみもの | 1 | 飲み物 | outros | bebida |
| 434 | かいしゃ | 1 | 会社 | lugares | empresa,companhia,firma |
| 331 | がっこう | 1 | 学校 | lugares | escola |
| 213 | まなぶ | 1 | 学ぶ | verbos | aprender |
| 337 | やすむ | 1 | 休む | verbos | descansar,repousar |
| 328 | かう | 1 | 買う | verbos | comprar |
| 655 | かいもの | 1 | 買い物 | atividades | compras,fazer compras |
| 273 | たつ | 1 | 立つ | verbos | levantar,levantar-se |
| 103 | がくせい | 1 | 学生 | escola | estudante |
| 656 | でんわ | 1 | 電話 | eletrônicos | telefone |
| 867 | でぐち | 1 | 出口 | estação | saída |
| 1099 | どくしょ | 1 | 読書 | escola | leitura |
| 1035 | いりぐち | 1 | 入口 | estação | entrada |
| 435 | たかい | 1 | 高い | adjetivos | alto,caro |
| 739 | こうこう | 1 | 高校 | escola | ensino médio |
| 381 | やすい | 1 | 安い | adjetivos | barato |
| 592 | すくない | 1 | 少ない | adjetivos | pouco |
| 1071 | しゃちょう | 1 | 社長 | pessoas | presidente,diretor |
| 411 | わかる | 1 | 分かる | verbos | entender,compreender |
| 130 | じかん | 1 | 時間 | tempo | horário |
| 76 | あいだ | 1 | 間 | advérbios | entre,durante |
| 708 | さき | 1 | 先 | outros | ponta |
| 955 | はんぶん | 1 | 半分 | outros | metade |
| 715 | てんいん | 1 | 店員 | pessoas | balconista,vendedor |
| 554 | がいこく | 1 | 外国 | lugares | país estrangeiro |
| 311 | でんしゃ | 1 | 電車 | veículos | trem |
| 1125 | みち | 1 | 道 | outros | estrada,rua,caminho |
| 1215 | どうろ | 1 | 道路 | outros | estrada,rua |
| 360 | ともだち | 1 | 友達 | pessoas | amigo |
| 373 | ゆうめい | 1 | 有名 | adjetivos | famoso |
| 624 | きんようび | 1 | 金曜日 | tempo | sexta-feira |
| 507 | えん | 1 | 円 | objetos | iene,yen |
| 939 | げんき | 1 | 元気 | adjetivos | bem,saldável |

Casos de atenção:

- **あげる (#503)**: hoje é só "dar". 上げる também é "levantar"; como os dois sentidos são do assunto *verbos* (uma forma 2 com o mesmo assunto nunca seria usada), o kanji foi para na forma 1 e acrescentar as alternativas.
- **はな (#364)** e **あめ (#505)**: o kanji vai só na forma 1 (flor, chuva). As formas 2 (nariz 鼻, doce 飴) ficam sem kanji: não vêm da página.
- **てん (#792)**: 天 só na forma 1 (céu); a forma 2 (ponto, 点) fica como está.
- **あし (#686)**: 足 vale para as duas formas (pé e perna).
- **はなみ (#345)**: a tradução cadastrada é "piquenique"; mantive, só ganha 花見.

## 6. Leituras de kanji (só as do item 8 foram aplicadas)

A página tem leituras que o banco não tem. O professor cadastrou só as leituras usadas no nível; acrescentar leitura muda a sessão (até 3 campos por kanji). Só recomendo as três usadas por palavras do sistema:

| Kanji | Banco | Página tem a mais | Recomendo |
|---|---|---|---|
| 上 | うえ,じょう | しょう,かみ,あ | +あ (上げる) |
| 下 | した,げ | か,さ | +さ (下げる) |
| 後 | のち,うし,あと | ご,こう | +ご (午後) |
| 一 | いち,ひと | いつ | — |
| 八 | はち,やっ,はっ | や | — |
| 九 | きゅう,ここの | く | — |
| 十 | じゅう,とお | と | — |
| 千 | せん | ち | — |
| 木 | もく,き | ぼく | — |
| 土 | ど,つち | と | — |
| 右 | みぎ,ゆう | う | — |
| 日 | にち,ひ,か,たち | じつ,び | — |
| 手 | て,た,しゃ | しゅ | — |
| 女 | おんな,め,じょ,にょう | にょ | — |
| 大 | おお,だい | たい | — |
| 言 | い,こと | げん,ごん | — |
| 飲 | の | いん | — |
| 聞 | き,ぶん | もん | — |
| 白 | しろ,はく,ぴゃく | びゃく | — |
| 金 | きん,かね | こん | — |

## 7. Correções de dados (aplicadas)

- #626 にちようび: romaji `nochiyoubi` → `nichiyoubi`.
- #939 げんき: tradução `bem,saldável` → `bem,saudável` (quem digitava certo levava erro).
- ひゃく estava cadastrada duas vezes (#470 e #618, idênticas e sem monitor de aluno): #618 excluída.
- 車: nível `n5` → `N5` (feito no item 2).

## 8. Leituras exigidas por nível e leituras que faltavam (aplicado no local e no Docker)

**Extensão ao sistema original.** Na Leitura de kanjis, o original pedia um campo por leitura cadastrada (até 3). Agora o sistema deduz, a partir das palavras de treino, qual leitura de cada kanji cada palavra usa (日曜日 = にち + よう + び, com sonorização e っ no fim). Uma leitura é exigida no nível L quando alguma palavra de nível ≤ L a usa. O nível da palavra é o mais alto entre os seus kanjis; kanji sem nível conta como N1.

- A sessão pede um campo por leitura exigida no nível escolhido (até 3). Sem leitura deduzida, pede 1 campo.
- A correção continua a do original (`TKanji.CheckReading`): qualquer leitura cadastrada vale.
- No erro, as leituras exigidas aparecem primeiro, em destaque, e as demais como "também valem".
- No cadastro de kanjis, o professor vê a partir de que nível cada leitura é exigida, as palavras que a justificam e as palavras que não se dividem.
- Código: `Nienow.Shinrikai.Kanji/KanjiReadingLevels.cs` (`ReadingRequirements`), testes em `tests/Nienow.Shinrikai.Tests/KanjiReadingLevelsTests.cs`. Rota nova: `GET /api/kanjis/readings?letter=` (professor).

Campos pedidos por sessão (banco local, depois das correções abaixo):

| Nível | Kanjis | Original | Com a regra |
|---|---|---|---|
| N5 | 103 | 241 | 149 |
| N4, N3, N2 | 225 | 398 | 276 |
| N1 | 357 | 675 | 419 |

**Leituras que faltavam** ([database/import/leituras-faltantes-n5.sql](../database/import/leituras-faltantes-n5.sql)). Das 49 palavras com kanji que não se dividiam, 13 eram por falta de leitura no cadastro:

| Kanji | Antes | Acrescentado | Palavras |
|---|---|---|---|
| 後 | のち,うし,あと | ご (go) | 午後 |
| 日 | にち,ひ,か,たち | じつ, に (jitsu, ni) | 休日; 日本, 日本語, 日本人, 日本国 |
| 上 | うえ,じょう | あ (a) | 上げる |
| 下 | した,げ | さ (sa) | 下げる |
| 四 | よん,よ,し | よっ (yo~tsu, como 六 むっ e 八 やっ) | 四日, 四つ |
| 六 | ろく,む,むっ,ろっ | むい (mui) | 六日 |
| 八 | はち,やっ,はっ | よう (you) | 八日 |

Também: #1035 いりぐち passa de 入口 (posto pela importação) para 入り口, que se divide em い + り + ぐち.

Ficam sem divisão, de propósito, 36 palavras:

- **Leitura especial (8):** 大人, 時計, 部屋, 上手, 一人, 二人, 明日, 今日. Não exigem leitura de nenhum kanji.
- **Kanji não cadastrado (28 palavras, 27 kanjis):** 彼, 次, 机, 供, 私, 緒, 葉, 達, 有, 富, 士, 港, 俳, 優, 腕, 否, 定, 的, 路, 珍, 昔, 漫, 筆, 側, 河, 鼻, 性. Cadastrar esses kanjis (leituras, significado, exemplos) é trabalho de conteúdo à parte.

Aplicado em 2026-11-22 no banco local e no Docker; backups de antes em `database/data/backup-2026-11-22-antes-leituras/`. **Azure: pendente.**

Visto de passagem: #408 じょうず tem romaji `jousu` (deveria ser `jouzu`); não corrigido.
