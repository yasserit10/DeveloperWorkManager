# Developer Work Manager

Arabic-first internal work-management system for a programming unit. It records projects, assigned work, progress updates, blockers, and report-ready history.

## Workflow

- The unit manager creates programmer accounts from `/team`, then assigns work from `/tasks`.
- Programmers sign in with their username, see only their assigned tasks, and record progress, hours, outcomes, and blockers.
- The manager or a report viewer can filter `/reports` by period and export the update register as an Excel-compatible CSV file.

## Technology

- ASP.NET Core / Blazor Server on .NET 10
- ASP.NET Core Identity for authentication and roles
- Entity Framework Core with Microsoft SQL Server

## Local development

The development connection uses LocalDB. To use another SQL Server without changing source code, set `ConnectionStrings__DefaultConnection` as an environment variable. Use `.env.example` only as a structure reference; never commit real credentials.

```powershell
dotnet ef database update
dotnet run
```

## UI assets

The Arabic RTL interface is styled with Tailwind CSS and SCSS, with Zain loaded from Google Fonts. Font Awesome is bundled locally under `wwwroot/lib/fontawesome`, so interface icons do not depend on an external icon CDN. Run the following after changing `.razor` classes or `Styles/app.scss`:

```powershell
npm.cmd run build:css
```

This regenerates `wwwroot/app.css`, which is the CSS file included in the production image.

## Production notes

1. Create a dedicated SQL Server login with access only to the `DeveloperWorkManager` database. Do not use the VPS administrator account for the application.
2. Place the production connection string in the server environment or a protected secret store.
3. Apply migrations as a deliberate deployment step: `dotnet ef database update`.
4. Bind SQL Server to localhost/private networking only; expose the application via HTTPS reverse proxy.
5. Schedule encrypted SQL backups and test restoring them.

## Ubuntu deployment

The application runs in Docker, while the existing Ubuntu Nginx installation is the public reverse proxy. SQL Server remains on the Ubuntu host and is never published through Docker.

1. Install Docker Engine and the Docker Compose plugin on Ubuntu.
2. Copy the project to `/opt/developer-work-manager`.
3. Confirm the DNS `A` record for `task.skdevs.uk` points to the VPS IP.
4. Copy `deploy/.env.production.example` to `.env`, then enter the application-specific SQL Server connection string.
5. Build and start it with:

```bash
docker compose up -d --build
```

6. The `migrate` service applies the EF migrations first; then the web service starts. Confirm both with `docker compose ps` and `docker compose logs migrate web`.
7. Copy `deploy/nginx/task.skdevs.uk.conf` to `/etc/nginx/sites-available/task.skdevs.uk`, enable it, and run `sudo nginx -t` before reloading Nginx.
8. Issue the certificate after the HTTP proxy works: `sudo certbot --nginx -d task.skdevs.uk`.

Only Nginx uses public ports 80 and 443; the application listens on `127.0.0.1:8088`. Do not expose port 1433 publicly; restrict it to localhost or a trusted private network.

Docker keeps ASP.NET Core Data Protection keys in the named `data-protection-keys` volume. Keep this volume during routine deployments so existing login cookies and antiforgery tokens remain valid.

The `BootstrapAdmin__*` values in `.env` create the first application administrator with the `UnitManager` role. Set a unique long password directly on the server, then remove the `BootstrapAdmin__Password` line after the first successful login.
