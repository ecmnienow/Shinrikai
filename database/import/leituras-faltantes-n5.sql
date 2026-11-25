-- Leituras que faltavam para as palavras de treino N5 se dividirem kanji a kanji
-- (ver docs/importacao-n5-joyokanjikai.md, seção "Leituras exigidas por nível").
-- Cada UPDATE só vale se o registro ainda estiver como antes; senão o script inteiro falha
-- (rodar com isql -b, que desfaz tudo em caso de erro).
--   isql -b -q -ch UTF8 -i leituras-faltantes-n5.sql <banco>
SET TERM ^ ;
EXECUTE BLOCK AS
BEGIN
  -- 午後 (ごご)
  UPDATE KANJI SET KNJ_KANA = 'のち,うし,あと,ご', KNJ_ROMAJI = 'nochi,ushi,ato,go'
   WHERE KNJ_KANJI = '後' AND KNJ_KANA = 'のち,うし,あと' AND KNJ_ROMAJI = 'nochi,ushi,ato';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_後';

  -- 休日 (きゅうじつ), 日本 / 日本語 / 日本人 / 日本国 (に + ほん)
  UPDATE KANJI SET KNJ_KANA = 'にち,ひ,か,たち,じつ,に', KNJ_ROMAJI = 'nichi,hi,ka,tachi,jitsu,ni'
   WHERE KNJ_KANJI = '日' AND KNJ_KANA = 'にち,ひ,か,たち' AND KNJ_ROMAJI = 'nichi,hi,ka,tachi';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_日';

  -- 上げる (あげる)
  UPDATE KANJI SET KNJ_KANA = 'うえ,じょう,あ', KNJ_ROMAJI = 'ue,jou,a'
   WHERE KNJ_KANJI = '上' AND KNJ_KANA = 'うえ,じょう' AND KNJ_ROMAJI = 'ue,jou';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_上';

  -- 下げる (さげる)
  UPDATE KANJI SET KNJ_KANA = 'した,げ,さ', KNJ_ROMAJI = 'shita,ge,sa'
   WHERE KNJ_KANJI = '下' AND KNJ_KANA = 'した,げ' AND KNJ_ROMAJI = 'shita,ge';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_下';

  -- 四日 (よっか), 四つ (よっつ); mesma convenção de 六 (むっ, mu~tsu) e 八 (やっ, ya~tsu)
  UPDATE KANJI SET KNJ_KANA = 'よん,よ,し,よっ', KNJ_ROMAJI = 'yon,yo,shi,yo~tsu'
   WHERE KNJ_KANJI = '四' AND KNJ_KANA = 'よん,よ,し' AND KNJ_ROMAJI = 'yon,yo,shi';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_四';

  -- 六日 (むいか)
  UPDATE KANJI SET KNJ_KANA = 'ろく,む,むっ,ろっ,むい', KNJ_ROMAJI = 'roku,mu,mu~tsu,ro~tsu,mui'
   WHERE KNJ_KANJI = '六' AND KNJ_KANA = 'ろく,む,むっ,ろっ' AND KNJ_ROMAJI = 'roku,mu,mu~tsu,ro~tsu';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_六';

  -- 八日 (ようか)
  UPDATE KANJI SET KNJ_KANA = 'はち,やっ,はっ,よう', KNJ_ROMAJI = 'hachi,ya~tsu,ha~tsu,you'
   WHERE KNJ_KANJI = '八' AND KNJ_KANA = 'はち,やっ,はっ' AND KNJ_ROMAJI = 'hachi,ya~tsu,ha~tsu';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_八';

  -- いりぐち: a importação N5 tinha posto 入口; 入り口 se divide em い + り + ぐち
  UPDATE VOCABULARY SET VCB_KANJI_1 = '入り口'
   WHERE VCB_ID = 1035 AND VCB_KANA = 'いりぐち' AND VCB_KANJI_1 = '入口';
  IF (ROW_COUNT <> 1) THEN EXECUTE STATEMENT 'FALHOU_入り口';
END^
SET TERM ; ^
COMMIT;
