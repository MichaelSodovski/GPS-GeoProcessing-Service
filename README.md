# GPS GeoProcessing Service

## Description
The GPS Geoprocessing Service is a robust .NET Core application tailored for advanced geospatial processing tasks. Its core functionality involves retrieving World Geodetic System (WGS) coordinates and accurately converting them to the Israeli Transverse Mercator (ITM) coordinate system. Leveraging the NetTopologySuite, a powerful library for spatial data processing, the service efficiently performs proximity analyses. It determines the relative distance of GPS coordinates, representing Israel's rail company wagons, to the nearest rail line. By setting a threshold of 3 meters, it can decisively associate each wagon with its corresponding rail line, thus offering the rail company a precise method for tracking wagon locations.

## Installation

To install this project, you'll need to have .NET installed on your machine. Follow these steps:

1. Clone the repository to your local machine using Git:
   ```sh
   git clone [repository-url]
