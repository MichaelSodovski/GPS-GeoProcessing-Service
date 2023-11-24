# GPS GeoProcessing Service

## Description
The GPS Geoprocessing Service is a robust .NET Core application tailored for advanced geospatial processing tasks. Its core functionality involves retrieving World Geodetic System (WGS) coordinates and accurately converting them to the Israeli Transverse Mercator (ITM) coordinate system. Leveraging the NetTopologySuite, a powerful library for spatial data processing, the service efficiently performs proximity analyses. It determines the relative distance of GPS coordinates, representing Israel's rail company wagons, to the nearest rail line. By setting a threshold of 3 meters, it can decisively associate each wagon with its corresponding rail line, thus offering the rail company a precise method for tracking wagon locations.

## Features
- **Continuous Coordinate Conversion**: This service takes a batch of 65 entries from the RAIL_LINE_DATA table in WGS format and converts them to ITM format using the NetTopologySuite library, ensuring accurate data translation for geographical information systems.
- **Proximity Analysis for Rail Line Assignment**: It measures the distance between the newly converted GPS points and predefined rail line shapes. This analysis is crucial for determining the association between a rail wagon and its corresponding rail line. The criteria for assignment are precise: if a GPS point is less than 3 meters from a rail line, it is considered to belong to that line.
- **Intelligent Mapping**: The service implements a smart mapping system that correlates GPS points from the RAIL_LINE_DATA table with the rail line numbers from the RAIL_LINES table. This association is based on proximity measurements, streamlining the process of identifying which rail wagon travels on which rail line.
- **Detailed Logging with Serilog**: Utilizes the robust Serilog logging framework to provide insightful details of the entire process. Logs include the conversion procedure, distance measurements, and the final mapping outcome. This level of logging is invaluable for auditing, troubleshooting, and refining the service.

The functionality of this service ensures that rail wagons are accurately tracked and mapped to their respective rail lines in real-time, providing a critical component for the management of railway operations.

## Technical Details
- **.NET Version**: The application is built using .NET 7.
- **Database**: Oracle Database. 
- **ORM**: Direct SQL commands with parameters to prevent SQL injection.
- **Libraries**: NetTopologySuite 2.5.0, Serilog 3.0.1. 

## Contributing

We welcome contributions and suggestions! Please fork the repository and create a new pull request for any features or bug fixes.
