# Shinrikai no Azure (Container Apps + VM do Firebird)

Esta pasta publica no Azure as mesmas imagens do `compose.yaml`, descritas em Bicep. É a única parte do
repositório ligada ao Azure, junto com o workflow `.github/workflows/deploy.yml`. O código do sistema não
depende dela.

```
Internet ─► shinrikai-web (Container App público, HTTPS)
               │  YARP, http://shinrikai-api
               ▼
            shinrikai-api (Container App só interno)
               │  VNet 10.10.0.0/16, porta 3050
               ▼
            vm-shinrikai-db (Ubuntu 24.04 + firebirdsql/firebird:5.0.4, banco num disco gerenciado)
```

## Arquivos

- `main.bicep` e `resources.bicep` criam o grupo `rg-shinrikai` e os recursos:
  - Log Analytics, Container Registry Basic e uma identidade gerenciada que baixa as imagens sem senha.
  - A identidade `id-shinrikai-github`, que o workflow de deploy usa por OIDC: só aceita tokens deste
    repositório no `master` e tem o papel Contributor no grupo.
  - VNet com uma sub-rede delegada aos Container Apps e outra para a VM.
  - VM `Standard_B2ats_v2` com IP privado fixo `10.10.2.4` e disco de dados de 8 GB, que sobrevive à
    exclusão da VM.
  - O NSG da VM só aceita a 3050 vinda da sub-rede dos apps. Nem SSH entra. O IP público da VM serve
    só para a saída (apt e Docker Hub).
  - Os dois apps escalam a zero sem acesso (0,25 vCPU e 0,5 GiB cada).
- `cloud-init.yaml` roda no primeiro boot da VM: instala o Docker, formata e monta o disco e sobe o
  Firebird com o mesmo `schema.sql`, `seed.sql` e `init-shinrikai.sh` do compose. O cloud-init não muda
  depois da criação da VM. Por isso o `deploy.ps1` passa `vmExists=true` quando ela já existe.
- `deploy.ps1`:
  1. Gera os segredos em `infra/.azure.env` (ignorado pelo git) e a chave SSH em `~/.ssh/shinrikai-azure`.
  2. Implanta o Bicep.
  3. Monta as imagens com o Docker local, envia ao registry e cria ou atualiza os apps.
  4. Grava as variáveis `AZURE_*` do workflow no repositório (`gh`). São ids, não segredos.

  A região padrão é `northcentralus`, a mais barata em que a assinatura oferece a série B.

## Implantar

Na raiz do repositório:

```powershell
az login
gh auth login
.\infra\deploy.ps1 -Preview     # what-if: mostra o que seria criado ou alterado
.\infra\deploy.ps1              # implanta; no fim mostra a URL https://shinrikai-web...
```

Depois do primeiro `deploy.ps1`, o workflow `.github/workflows/deploy.yml` roda a cada Build com sucesso
no `master`: monta as imagens, envia ao registry e atualiza os Container Apps. O login no Azure é por
credencial federada, sem segredo guardado no GitHub. Mudanças nesta pasta continuam sendo implantadas
pelo `deploy.ps1`.

## Administrar

Para administrar o Firebird sem SSH, use `az vm run-command`:

```powershell
az vm run-command invoke -g rg-shinrikai -n vm-shinrikai-db --command-id RunShellScript --scripts "docker logs --tail 20 firebird"
```

## Desligar

Para parar os custos de vez, apague o grupo de recursos (o disco do banco vai junto) e a variável que
habilita o workflow. Sem `AZURE_CLIENT_ID`, o `deploy.yml` é pulado e os pushes no `master` não falham:

```powershell
az group delete -n rg-shinrikai --yes
gh variable delete AZURE_CLIENT_ID
```

Esta pasta pode continuar no repositório como referência. Para removê-la, marque uma tag antes
(`git tag azure-v1`) e apague `infra/` e o `deploy.yml` num único commit.
