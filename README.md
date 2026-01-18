# Project Setup Guide

This guide explains how to set up the PostgreSQL database with Docker, initialize it, run the .NET solution, and test the API.

---

## 1. Start PostgreSQL with Docker

Start a PostgreSQL container using Docker.  
In this setup we use:

- Container name: `swen-db`
- Image: `postgres:latest`
- Port mapping: `5432:5432`
- Password: `1234`

Run this command in your terminal:

```docker run --name swen-db -e POSTGRES_PASSWORD=1234 -p 5432:5432 -d postgres:latest```

Make sure the container is running before continuing.

---

## 2. Initialize the Database (pgAdmin4)

The SQL schema and initial setup are provided in `pgsetup.txt`.

### 2.1 Open pgAdmin4

1. Start **pgAdmin4**.
2. Create a new server connection with the following data:

- Name: `swen-db`
- Host name/address: `host.docker.internal`  
  (depending on your environment, `localhost` may also work)
- Port: `5432`
- Maintenance database: `postgres`
- Username: `postgres`
- Password: `1234`

### 2.2 Run the SQL script

1. Open the file `pgsetup.txt` from the project directory.
2. In pgAdmin4, open the **Query Tool** for your `postgres` database.
3. Paste the contents of `pgsetup.txt` into the query editor.
4. Execute the query to create and initialize the database objects.

---

## 3. Start Services in the Correct Order

1. Start the **PostgreSQL Docker container** (`swen-db`) if it is not already running.
2. Then start **pgAdmin4** (if you need to inspect the database).

---

## 4. Run the .NET Solution

1. Open the `.sln` file in your preferred IDE (e.g., Visual Studio or Rider).
2. Restore NuGet packages if necessary.
3. Build the solution.
4. Run the application (F5 or your IDE’s run/debug button).

Make sure the application can connect to the PostgreSQL instance on port `5432` with the credentials configured above.

---

## 5. Test the API via .bat cURL Scripts

The project provides `.bat` scripts that call the API using `curl`.  
Run them in the following recommended order:

1. `admin-setup.bat`
2. `media-entry-test.bat`
3. `allaround-integration-test.bat`

Run each script from a terminal in the directory where the `.bat` files are located (usually the project root or a `scripts` folder).

If all scripts execute successfully (no errors from `curl` and expected responses from the API), your setup is working correctly.
