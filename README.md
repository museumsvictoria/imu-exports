Imu Exports
=========================== 

.Net Framework (4.7.2) console application to allow exporting of data from EMu collection management system. Uses the IMu .net API to allow querying and data retrieval.

### Configuration

All configuration settings located in the App.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.7.2" />
  </startup>
  <appSettings>
    <add key="EmuServerHost" value="bunjil.mv.vic.gov.au" />
    <add key="EmuServerPort" value="40022" />
    <add key="serilog:minimum-level" value="Debug" />
    <add key="serilog:enrich:with-property:Environment" value="Development" />
    <add key="serilog:enrich:with-property:Application" value="Imu Exports" />
    <add key="SeqUrl" value="http://godot.mv.vic.gov.au:5341" />
```

### Usage

Specify the type of export run by changing the cli parameters.  If no parameters are specified a list of the available options will be displayed on the CLI.  Futher parameters can also be specified that are specific to the export chosen.

#### Options

* `ala` - Exports records for the Atlas of Living Australia.
* `atd` - Exports records for the Atlas of Living Australia (Tissue data only).
* `gip` - Exports records for Gippsland Field Guide.
* `gun` - Export records for Gunditjmara Field Guide.
* `wc` - Export records for the Wikimedia commons.
* `ei` - One of export of images for Ursula.
* `io` - Extract records for Inside Out.

#### ala specific

* `-d, --dest`: Required. Destination directory for csv and images.
* `-a, --modified-after`: Get all records after modified date >= (yyyy-mm-dd format)
* `-b, --modified-before`: Get all records before modified date <= (yyyy-mm-dd format)