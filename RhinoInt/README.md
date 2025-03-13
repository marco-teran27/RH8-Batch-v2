# RH8-Batch-v2

## Overview

RH8-Batch-v2 is a Rhino 8 plugin for batch processing of 3D model files (.3dm) with Python or Grasshopper scripts. The application is designed to automate workflows by allowing users to process multiple files based on patient IDs, handling complex file naming conventions and batch execution rules.

## Features

- Process multiple Rhino (.3dm) files in batch mode
- Support for both Python and Grasshopper scripts
- Configurable file selection based on patient IDs and keywords
- Reprocessing options (ALL, PASS, FAIL, RESUME)
- Timeout management for script execution
- Detailed logging and validation
- Modern dependency injection architecture

## System Requirements

- Rhino 8 (Windows)
- .NET 7.0

## Installation

1. Download the latest release from the [Releases](https://github.com/yourusername/RH8-Batch-v2/releases) page
2. Extract the ZIP file to a location of your choice
3. In Rhino 8, run the command `_PlugInManager`
4. Click "Install..." and navigate to the extracted folder
5. Select the `RhinoInt.rhp` file
6. Restart Rhino

Alternatively, you can build from source:

1. Clone the repository: `git clone https://github.com/yourusername/RH8-Batch-v2.git`
2. Open the solution in Visual Studio 2022
3. Build the solution
4. Copy the output files to your Rhino plugins folder

## Usage

### Getting Started

1. Launch Rhino 8
2. Run the command `BatchProcessor` in the Rhino command line
3. Select a configuration file (JSON format)
4. The batch processor will validate the configuration and begin processing files

### Configuration File Format

Configuration files should be named according to the pattern `config-[ProjectName].json` and follow this structure:

```json
{
  "projectName": "ProjectName",
  "directories": {
    "file_dir": "C:\\Path\\To\\RhinoFiles",
    "output_dir": "C:\\Path\\To\\Output",
    "script_dir": "C:\\Path\\To\\Scripts"
  },
  "script_settings": {
    "script_name": "MyScript",
    "script_type": "Python"  // Options: "Python", "Grasshopper", "GH", "gh", "PY", "py"
  },
  "rhino_file_name_settings": {
    "mode": "list",  // Options: "list", "all"
    "keywords": ["keyword1", "keyword2"]
  },
  "pid_settings": {
    "mode": "list",  // Options: "list", "all"
    "pids": ["123456L-S12345", "654321R-S54321"]
  },
  "timeout_settings": {
    "minutes": 8
  },
  "reprocess_settings": {
    "mode": "ALL",  // Options: "ALL", "PASS", "FAIL", "RESUME"
    "reference_log": "C:\\Path\\To\\Previous\\Log.json"
  }
}
```

### Patient ID Format

Patient IDs must follow the format: `XXXXXXL-SXXXXX` or `XXXXXXR-SXXXXX` where:
- `X` represents a digit
- `L` or `R` indicates left or right
- `S` or `R` prefix followed by 5 digits

Example: `123456L-S12345`

### File Naming Convention

Rhino files (.3dm) should follow the naming pattern:
`[PatientID]-[Keyword]-v[Version]-[Suffix].3dm`

Example: `123456L-damold-v2-S12345.3dm`

## Project Structure

The solution is organized into several projects:

- **RhinoInt**: The Rhino plugin interface
- **Core**: Core business logic and orchestration
- **Commons**: Common utilities, parameters, and logging
- **Config**: Configuration parsing and validation
- **FileDir**: File directory parsing and validation
- **Interfaces**: Interface definitions
- **DInjection**: Dependency injection configuration

## Development

### Prerequisites

- Visual Studio 2022
- .NET 7.0 SDK
- Rhino 8 WIP

### Building

1. Open the solution in Visual Studio 2022
2. Restore NuGet packages
3. Build the solution

### Adding New Features

1. Identify the appropriate project for your feature
2. Implement the necessary interfaces
3. Register any new services in `DInjection/ServiceConfigurator.cs`
4. Update unit tests as needed

## Scripting

### Python Scripts

Python scripts should set a document string variable when complete:

```python
import scriptcontext
# Your code here
scriptcontext.doc.Strings.SetString('ScriptDone', 'true')
```

### Grasshopper Scripts

Grasshopper scripts can indicate completion by:

1. Setting a document string variable:
```
scriptcontext.doc.Strings.SetString('ScriptDone', 'true')
```

2. Creating a marker file:
```
System.IO.File.WriteAllText(
  System.IO.Path.Combine(
    System.IO.Path.GetTempPath(), 
    "RhinoGHBatch", 
    rhinoDocName + "_complete.marker"
  ), 
  ""
)
```

## Troubleshooting

### Common Issues

1. **Configuration validation fails**: Check your config file against the example format
2. **No files found**: Verify file_dir path and that file names match the expected pattern
3. **Script execution fails**: Check script path and format, and ensure it sets the completion flag
4. **Timeout errors**: Increase timeout_minutes in configuration

### Logging

The application logs details to the Rhino console. Check there for error messages and debug information.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributors

- TomiDoki - Original author

## Acknowledgments

- The Rhino development team
- McNeel for the Rhino API