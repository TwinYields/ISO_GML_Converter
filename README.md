# ISO_GML_Converter
Convert completed ISOBUS (ISO 11783) Task file Timelog data to GML or CSV format.
The geometry information of tractor-implement system is extracted from device description. Using the geometry information, the actual trajectory of device elements are calculated using simulation and it is used in converted log files instead of raw GNSS position.


Usage:
ISO_GML_Converter [TASKDATA.XML] [-output={GML|CSV}]
