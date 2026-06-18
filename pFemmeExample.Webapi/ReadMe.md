If you are creating a new webapi project, then:

- Create folders ‘Hubs’ and 'Services' (see ReadMe.md in this folders)

- Create 'Endpoints.cs' file in the root of the project and set your endpoints


TESTING WEBAPI
--------------
- The file Testing.md contains test examples for registration and querying via WebAPI.
- Scalar UI is integrated into the WebAPI project, allowing you to test all endpoints from the WebAPI before publishing it.


CLOUD SETUP
-----------
- Order a domain or subdomain from your hosting provider (e.g. Azure or private hosting providers like firestorm.ch).
- Create an MSSQL database with your hosting provider. Use the same parameters as when creating the database locally (e.g., Database=db_pfemmeexample, User_ID=dbuser_db_pfemmeexample and Password=YOUR_SECURE_PASSWORD).
- Connect remotely to the database from your local Windows computer using MSSQL Management Studio.
- Run the scripts CREATE_TABLES.sql / CRUD.sql (located in the directory pFemmeExample.Shared > Db)
- Upload your WebAPI (from Visual Studio direct publishing to Azure or zipping and uploading/unzipping to the hosting directory),
- Test webapi connection by calling the endpoint https://yourdomain.com/api/unauthorizedconnection (replace 'yourdomain.com' with your actual domain). You should receive the response "Successful unauthorized connection to the Webapi".