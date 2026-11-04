/*
 * Shinrikai — dados iniciais de um banco novo.
 *
 * - NOTIFICATIONS: textos de "Ajuda" das telas (IDs usados pelo front: 1, 3, 4, 5, 6, 7).
 *   Os textos originais estão no banco antigo; ao migrá-lo, eles substituem estes.
 * - SUBJECTS: assuntos iniciais do vocabulário. O sistema não tem tela de cadastro de assuntos
 *   (o menu "Cadastro de assuntos" estava desativado no original) — inclua outros com:
 *     INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Nome');
 */

SET SQL DIALECT 3;

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (1,
'<p><b>Transcrição para hiragana</b></p>
<p>Escolha um assunto. Para cada palavra escrita em kanji, digite a leitura em <b>hiragana</b> e clique em <b>Corrigir</b>.</p>
<p>As palavras erradas voltam no modo <b>Refazer erradas</b> até você acertá-las várias vezes seguidas.</p>');

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (3,
'<p><b>Estudo de gramática</b></p>
<p>Os conteúdos estão agrupados por nível do JLPT. Os marcados com cadeado ainda não estão disponíveis: clique neles para ver o que precisa estudar antes.</p>
<p>Ao ler um conteúdo por alguns instantes, ele é considerado estudado e libera os conteúdos que dependem dele. Quando houver, use <b>Exercícios</b> para praticar.</p>');

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (4,
'<p><b>Leitura de textos</b></p>
<p>As palavras sublinhadas estão no vocabulário. A leitura (furigana) das palavras que você ainda não estudou fica escondida: <b>clique na palavra</b> para vê-la.</p>
<p>As palavras que você clicar passam a ser revisadas até você aprendê-las.</p>');

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (5,
'<p><b>Resolução de exercícios</b></p>
<p>Responda os exercícios em sequência: escolha uma alternativa e clique em <b>Corrigir</b>. O exercício seguinte é liberado após responder o atual.</p>
<p>Ao final é mostrado o total de acertos.</p>');

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (6,
'<p><b>Estudo de vocabulário</b></p>
<p>Escolha um assunto e a direção (japonês para português ou português para japonês), escreva as traduções e clique em <b>Corrigir</b>.</p>
<p>Use <b>Repetir</b> para embaralhar a página e <b>Refazer erradas</b> para estudar só as palavras que você errou.</p>');

INSERT INTO NOTIFICATIONS (NTF_ID, NTF_MESSAGE) VALUES (7,
'<p><b>Leitura de kanjis</b></p>
<p>Escolha o nível do JLPT. Para cada kanji, escreva suas leituras em kana (uma por campo) e clique em <b>Corrigir</b>. O kanji só conta como acerto com todas as leituras certas.</p>
<p>Os kanjis errados voltam no modo <b>Refazer erradas</b>.</p>');

INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Geral');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Animais');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Comida');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Cores');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Corpo');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Família');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Números');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Tempo');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Lugares');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Verbos');
INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (NEXT VALUE FOR SUBJECTS_ID_GEN, 'Adjetivos');

COMMIT;
