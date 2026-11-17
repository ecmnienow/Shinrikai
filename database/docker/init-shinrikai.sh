#!/bin/bash
# Executado pela imagem firebirdsql/firebird (/docker-entrypoint-initdb.d) só quando o banco é criado.
# Roda os mesmos schema.sql e seed.sql do create-database.ps1. Não usa o tratamento automático de .sql
# da imagem porque ele não passa "-ch UTF8", e o seed tem acentos.
# Pode ser executado ou carregado com "source" pelo entrypoint: por isso "exit" e não "set -e".

for script in schema.sql seed.sql; do
    echo "Shinrikai: executando $script"
    /opt/firebird/bin/isql -b -q -ch UTF8 -u "$FIREBIRD_USER" -p "$FIREBIRD_PASSWORD" \
        -i "/shinrikai/$script" "$FIREBIRD_DATABASE" || exit 1
done
