# WSL2 + Docker Environment (Windows)

**Last updated**: November 2026
**Purpose**: Reproduce the local Docker environment used to run the SeldonVigil compose stack on Windows — WSL2 + Ubuntu + Docker Engine + PostgreSQL, without Docker Desktop.

This is the environment the compose stack is developed and tested in. It is not required for hosting; see [HOSTING_REQUIREMENTS.md](../HOSTING_REQUIREMENTS.md) for the deployment path.

---

## Overview

- WSL2 with Ubuntu 26.04 LTS, installed to a custom folder (the VHDX lives there, not in the default WSL location).
- Docker Engine 29.9.3 and docker compose v5.7.0, running inside WSL.
- PostgreSQL 18 with the pgvector extension, running inside WSL.
- The SeldonVigil compose stack runs entirely inside WSL; its published ports are reachable from Windows at `localhost`.

Docker Desktop is deliberately not used: it adds a second VM layer, a GUI and a licensing question that a local test environment does not need.

---

## 1. Install WSL2 and a distribution

From an elevated PowerShell (administrator):

```powershell
wsl --unregister Ubuntu          # only if a stale registration exists
$InstallDir = "D:\WSL\Ubuntu"    # any folder you like
mkdir $InstallDir
wsl --install Ubuntu --location $InstallDir --no-launch
```

- `--location` puts the distribution's VHDX in that folder. Create the folder first; WSL does not.
- `--no-launch` installs without starting, so the first-run username prompt does not race the install.
- Launch once and create the Unix user: `wsl -d Ubuntu`.

If a distribution was registered against a VHDX that no longer exists, WSL fails with
`Failed to attach disk '...ext4.vhdx': ERROR_PATH_NOT_FOUND`. Unregister it and reinstall as above.

## 2. Configure .wslconfig

`%UserProfile%\.wslconfig`:

```ini
[general]
# Keep the distro instance alive after the last shell closes
instanceIdleTimeout=-1

[wsl2]
swap=2GB
processors=4
memory=12GB
# Keep the VM alive instead of shutting down when idle
vmIdleTimeout=-1
```

**There are two timeouts, and both matter:**

- `instanceIdleTimeout` (default 15 s) terminates the distro instance after the last shell closes. systemd-logind then powers the VM off, which stops the Docker daemon and every container 30-90 seconds later.
- `vmIdleTimeout` (default 60 s) shuts the VM down when it is idle.

Setting only one of them leaves the containers stopping when no terminal is open. Set both to `-1`.

Apply the file with `wsl --shutdown`, wait a few seconds, then relaunch.

## 3. Install Docker Engine inside WSL

```bash
sudo apt update && sudo apt install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu resolute stable" | sudo tee /etc/apt/sources.list.d/docker.list >/dev/null
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Use the distro codename (`resolute` for Ubuntu 26.04) and confirm
`https://download.docker.com/linux/ubuntu/dists/<codename>/Release` returns 200 before adding the repository.

Enable systemd, start Docker and add the user to the docker group:

```bash
printf '[boot]\nsystemd=true\n\n[user]\ndefault=<you>\n' | sudo tee /etc/wsl.conf
# from PowerShell: wsl --shutdown, then relaunch
sudo systemctl enable --now docker
sudo usermod -aG docker $USER
```

## 4. Install PostgreSQL inside WSL

The compose stack expects PostgreSQL at `localhost:5432` and reaches it through
`host.docker.internal`. Inside WSL that name resolves to the Docker bridge gateway
**inside the WSL VM**, not the Windows host — so PostgreSQL must run inside WSL
(or the connection string must be changed).

```bash
sudo apt install -y postgresql postgresql-contrib postgresql-18-pgvector

CONF=/etc/postgresql/18/main/postgresql.conf
HBA=/etc/postgresql/18/main/pg_hba.conf
sudo sed -i "s|^#\?listen_addresses.*|listen_addresses = '*'|" "$CONF"
echo "host    all             all             172.16.0.0/12          scram-sha-256" | sudo tee -a "$HBA" >/dev/null
sudo systemctl enable --now postgresql
```

Docker assigns compose networks from 172.17, 172.18, ... sequentially, so allow the
whole `172.16.0.0/12` range rather than a single subnet.

Set the password and create the database from the repository's `.env` (never type the
password into a shell command; read it from the file):

```bash
cd /mnt/<drive>/SeldonVigil/infrastructure/docker   # or the copy in ~/SeldonVigil
set -a; . ./.env; set +a
printf "ALTER USER postgres PASSWORD '%s';\n" "${POSTGRES_PASSWORD//\'/\'\'}" | sudo -u postgres psql -v ON_ERROR_STOP=1
sudo -u postgres createdb seldonvigil
```

Verify from a container:

```bash
docker run --rm --add-host host.docker.internal:host-gateway alpine nc -z -w 5 host.docker.internal 5432
```

## 5. Reachability from Windows

WSL localhost forwarding is on by default, so the ports published by compose are
reachable on the Windows host at `localhost`:

| URL | Service |
|-----|---------|
| `http://localhost` | Web UI |
| `http://localhost:8080/healthz` | API health |
| `http://localhost:9090` | Prometheus |

Verify from Windows, not only from inside WSL:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8080/healthz
```

## 6. Run the SeldonVigil stack

Build-heavy images (`npm ci`, `dotnet restore`) are slow on `/mnt/<drive>` (DrvFs).
Clone the repository into the WSL filesystem before building:

```bash
git clone /mnt/<drive>/SeldonVigil ~/SeldonVigil
cp /mnt/<drive>/SeldonVigil/infrastructure/docker/.env ~/SeldonVigil/infrastructure/docker/.env
cd ~/SeldonVigil/infrastructure/docker
sudo docker compose up -d --build
```

Then open <http://localhost>.

## 7. Verification

```bash
sudo docker compose ps                                   # every service running
curl http://localhost:8080/healthz                       # {"status":"Healthy",...}
curl -o /dev/null -w '%{http_code}\n' http://localhost   # 200
```

An API route that requires authentication returning 401 is expected, not a failure.

## 8. Management

```bash
cd ~/SeldonVigil/infrastructure/docker
sudo docker compose ps
sudo docker compose logs -f api
sudo docker compose restart api
sudo docker compose down
sudo docker compose up -d --build
```

---

## Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Containers stop 30-90 s after closing the terminal | WSL idle timeouts | Set `instanceIdleTimeout=-1` and `vmIdleTimeout=-1` in `.wslconfig`, then `wsl --shutdown` |
| `Failed to attach disk '...ext4.vhdx'` | Stale distro registration | `wsl --unregister <distro>` and reinstall |
| `no pg_hba.conf entry for host "172.18.x.x"` | Compose network is not 172.17 | Allow `172.16.0.0/12` in `pg_hba.conf` and reload PostgreSQL |
| `psql: syntax error at or near ":"` | psql variable interpolation mangled through `wsl --exec` | Pipe the SQL: `printf ... \| sudo -u postgres psql` |
| `docker ps` empty but `docker compose ps` shows containers | Docker daemon socket activation after a restart | Check `systemctl status docker`; the daemon starts on demand |
| Builds are very slow | Running from `/mnt/<drive>` (DrvFs) | Clone into `~/` and build there |

---

## Related

- [HOSTING_REQUIREMENTS.md](../HOSTING_REQUIREMENTS.md) — self-hosted deployment
- [Redpanda runbook](../runbooks/redpanda.md) — broker operations
