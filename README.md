# nav-applicationarea
A tool we use at [Singhammer IT Consulting](https://www.singhammer.com/) to change the ApplicationArea property in a batch of Dynamics NAV Objects.

## General
This small utility processes [Microsoft Dynamics NAV](https://www.microsoft.com/en-us/dynamics365/nav-overview) objects and sets or resets the ApplicationArea on Actions, Fields, Parts, and MenuItems.

## Usage
The tool can either set or reset the area. An ID range can be specified optionally.

It is using an all-or-nothing approach, i.e. it supports either an empty ApplicationArea or one with exactly one area set. This means it will only set the property on controls that previously had nothing set or reset the property back to empty for all controls that had a specific area.

### Setting the area
`ApplicationArea.exe directory\*.txt -set AREA [-minId nnnnnn] [-maxId nnnnnn]`

* Parameter 1 (* directory\\*.txt): Specifies a path and file pattern of text files that should be processed
* Parameter 2 (-set AREA): Sets all empty ApplicationArea properties to AREA
* Parameter 3 (-minId nnnnnn): Optional. Process only controls starting with (and including) ID nnnnnn
* Parameter 4 (-maxId nnnnnn): Optional. Process only controls up to (and including) ID nnnnnn

*Note: Text files can contain one or multiple objects. So you can have either one file per object or multiple objects in one text file.

### Resetting the area
`ApplicationArea.exe directory\*.txt -reset AREA [-minId nnnnnn] [-maxId nnnnnn]`

(Same as above, but with parameter `-reset`)

## Example

1. Export a couple of pages from the C/SIDE client to c:\temp\pages.txt
1. Set the area to MYAREA for all controls starting at ID 1000000000<br>
`ApplicationArea.exe c:\temp\pages.txt -set MYAREA -minId 1000000000`

## Notes on compiling from source code

* Should compile with Visual Studio 2015 or similar
* Several Dynamics NAV DLLs must be copied from the "RoleTailored Client" folder into the same folder as `ApplicationArea.exe`
  * Microsoft.Dynamics.Nav.Model.dll
  * Microsoft.Dynamics.Nav.Model.Parser.dll
  * Microsoft.Dynamics.Nav.Model.Tools.dll
  * Microsoft.Dynamics.Nav.Model.TypeSystem.dll

## Binary release

If you do not want to compile the source code but run the tool anyway, you are free to download the releases provided in this repository. Please do note that I am not distributing the DLLs that this tool depends on. You can copy them from your Dynamics NAV installation - see the included Readme.txt for details.
