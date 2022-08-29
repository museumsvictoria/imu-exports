# IMu Exports
 
.Net 6 worker service CLI application which is used to export data from EMu for use in external projects.

EMu is a collection management system primarily used in museums, this application utilizes the IMu .Net API to query and retrieve data from the EMu system.

Specific exports are located within the tasks folder and inherit from the ImuTaskBase, a base class that allows data extraction from EMu in a standardised manner.

## Configuration

All configuration settings are located in the appsettings.json.

```json
{
  "AppSettings" : {
    "LiteDbFilename": "database.db",
    "Emu": {
      "Host": "host",
      "Port": 0
    },
    "AtlasOfLivingAustralia": {
      "Host": "host",
      "Username": "username",
      "Password": "password"
    },
    "AusGeochem": {
      "BaseUrl": "baseurl",
      "Username": "username",
      "Password": "password",
      "DataPackages": [
        {
          "Id": 0,
          "Discipline": ""
        }
      ],
      "ArchiveId": 0
    }
  }
}
```

## Usage

Specify the type of export run by selecting the specific commands listed below.  If no commands are specified a list of the available options will be displayed.  Further options can also be specified that are specific to the command chosen.

### Commands

* `ala` - Exports MV collection records for the Atlas of Living Australia (<https://www.ala.org.au/>). Records are exported from EMu and transformed into a Darwin Core Archive package which is either exported to a specified directory or directly uploaded to an FTP server.  
* `agn` - Export MV collection records for AusGeochem. (<https://ausgeochem.auscope.org.au/>).  Utilizes the AusGeochem REST API in order to directly send records. _Note_: an embedded CSV is used to match MV material names with AusGeochem material names.

### ala specific options (Atlas of Living Australia)

* `-d, --dest`
  
  Destination directory for csv and images. If not set, export is assumed to be automated, in which case the data will be uploaded to the ftp host specified in the AtlasOfLivingAustralia section within AppSettings.

 
* `-a, --modified-after`

  Get all EMu records after modified date >= (yyyy-mm-dd format).


* `-b, --modified-before`
 
  Get all EMu records before modified date <= (yyyy-mm-dd format).

### agn specific options (AusGeochem)

* `-d, --delete-all`

  Deletes all samples in AusGeochem specified in the DataPackages section of AppSettings.