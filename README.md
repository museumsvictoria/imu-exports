Imu Exports
=========================== 

.Net 6 worker service application which is used to export data from EMu for use in various projects. Uses the IMu .net API to allow querying and data retrieval.  Solution also contains the older .Net Framework 4.7.2 project which contains exports that are no longer used.

Specific exports are located within the tasks folder and inherit from the ITask interface.

### Configuration

All configuration settings located in the appsettings.json

```json
{
  "AppSettings" : {
    "LiteDbFilename": "database.db",
    "Emu": {
      "Host": "bunjil.mv.vic.gov.au",
      "Port": "40022"
    },
    "AtlasOfLivingAustralia": {
      "Host": "host",
      "Username": "username",
      "Password": "password"
    }
  }
}
```

### Usage

Specify the type of export run by changing cli parameters.  If no parameters are specified a list of the available options will be displayed on the CLI.  Futher parameters can also be specified that are specific to the export chosen.

#### Options

* `ala` - Exports records for the Atlas of Living Australia.
* `agn` - Export records for AusGeochem.

### ala specific
Note: If --dest not set export assumed to be automated, in which case the data will be uploaded to the ftp host specified in the AtlasOfLivingAustralia section within AppSettings.  

* `-d, --dest`: Required. Destination directory for csv and images.
* `-a, --modified-after`: Get all records after modified date >= (yyyy-mm-dd format)
* `-b, --modified-before`: Get all records before modified date <= (yyyy-mm-dd format)