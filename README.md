# Workshop Management API

ASP.NET Core 8 backend for workshop inventory and production management system.

## Features

- Material management (CRUD, stock tracking)
- Material receipts and inventory
- Product recipes with BOM (Bill of Materials)
- Production with automatic FIFO material write-off
- Finished products management (sales, write-offs)
- Reports and dashboard
- Operation history

## Tech Stack

- ASP.NET Core 8
- Entity Framework Core
- PostgreSQL
- Swagger/OpenAPI

## API Endpoints

- `GET /api/materials` - List materials
- `GET /api/materialreceipts` - List receipts
- `GET /api/products` - List products/recipes
- `GET /api/productions` - List productions
- `GET /api/finishedproducts` - List finished products
- `GET /api/reports/dashboard` - Dashboard data
- `GET /api/history` - Operation history

## Environment Variables

| Variable | Description |
|----------|-------------|
| `DATABASE_URL` | PostgreSQL connection string |
| `ALLOWED_ORIGINS` | Comma-separated list of allowed CORS origins |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) |

## Running Locally

```bash
cd WorkshopApi
dotnet restore
dotnet run
```

API will be available at http://localhost:5000
Swagger UI: http://localhost:5000/swagger

## Docker

```bash
docker build -t mat-backend ./WorkshopApi
docker run -p 8080:8080 -e DATABASE_URL="your_connection_string" mat-backend
```

## Deploy to Render.com

1. Connect this repository to Render
2. Select "Docker" environment
3. Set Dockerfile path: `./WorkshopApi/Dockerfile`
4. Set Docker context: `./WorkshopApi`
5. Add environment variables

## License

MIT
