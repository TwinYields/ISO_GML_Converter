// NOTE: This file is originally published in: https://gist.github.com/govert/1b373696c9a27ff4c72a
// The original code is modified for the purpose of this project


using System;
using System.Diagnostics;
using static System.Math;

// Some helpers for converting GPS readings from the WGS84 geodetic system to a local North-East-Up cartesian axis and back

// The implementation here is according to the paper:
// "Conversion of Geodetic coordinates to the Local Tangent Plane" Version 2.01.
// "The basic reference for this paper is J.Farrell & M.Barth 'The Global Positioning System & Inertial Navigation'"
// Also helpful is Wikipedia: http://en.wikipedia.org/wiki/Geodetic_datum
// Also helpful are the guidance notes here: http://www.epsg.org/Guidancenotes.aspx
public class GpsUtils
{

    // WGS-84 geodetic constants
    const double a = 6378137.0;         // WGS-84 Earth semimajor axis (m)

    const double b = 6356752.314245;     // Derived Earth semiminor axis (m)
    const double f = (a - b) / a;           // Ellipsoid Flatness
    const double f_inv = 1.0 / f;       // Inverse flattening

    //const double f_inv = 298.257223563; // WGS-84 Flattening Factor of the Earth 
    //const double b = a - a / f_inv;
    //const double f = 1.0 / f_inv;

    const double a_sq = a * a;
    const double b_sq = b * b;
    const double e_sq = f * (2 - f);    // Square of Eccentricity

    // Converts WGS-84 Geodetic point (lat, lon, h) to the 
    // Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z).
    public static void GeodeticToEcef(double lat, double lon, double h,
                                        out double x, out double y, out double z)
    {
        // Convert to radians in notation consistent with the paper:
        var lambda = DegreesToRadians(lat);
        var phi = DegreesToRadians(lon);
        var s = Sin(lambda);
        var N = a / Sqrt(1 - e_sq * s * s);

        var sin_lambda = Sin(lambda);
        var cos_lambda = Cos(lambda);
        var cos_phi = Cos(phi);
        var sin_phi = Sin(phi);

        x = (h + N) * cos_lambda * cos_phi;
        y = (h + N) * cos_lambda * sin_phi;
        z = (h + (1 - e_sq) * N) * sin_lambda;
    }

    // Converts the Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z) to 
    // (WGS-84) Geodetic point (lat, lon, h).
    public static void EcefToGeodetic(double x, double y, double z,
                                        out double lat, out double lon, out double h)
    {
        var eps = e_sq / (1.0 - e_sq);
        var p = Sqrt(x * x + y * y);
        var q = Atan2((z * a), (p * b));
        var sin_q = Sin(q);
        var cos_q = Cos(q);
        var sin_q_3 = sin_q * sin_q * sin_q;
        var cos_q_3 = cos_q * cos_q * cos_q;
        var phi = Atan2((z + eps * b * sin_q_3), (p - e_sq * a * cos_q_3));
        var lambda = Atan2(y, x);
        var v = a / Sqrt(1.0 - e_sq * Sin(phi) * Sin(phi));
        h = (p / Cos(phi)) - v;

        lat = RadiansToDegrees(phi);
        lon = RadiansToDegrees(lambda);
    }

    // Converts the Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z) to 
    // East-North-Up coordinates in a Local Tangent Plane that is centered at the 
    // (WGS-84) Geodetic point (lat0, lon0, h0).
    public static void EcefToEnu(double x, double y, double z,
                                    double lat0, double lon0, double h0,
                                    out double xEast, out double yNorth, out double zUp)
    {
        // Convert to radians in notation consistent with the paper:
        var lambda = DegreesToRadians(lat0);
        var phi = DegreesToRadians(lon0);
        var s = Sin(lambda);
        var N = a / Sqrt(1 - e_sq * s * s);

        var sin_lambda = Sin(lambda);
        var cos_lambda = Cos(lambda);
        var cos_phi = Cos(phi);
        var sin_phi = Sin(phi);

        double x0 = (h0 + N) * cos_lambda * cos_phi;
        double y0 = (h0 + N) * cos_lambda * sin_phi;
        double z0 = (h0 + (1 - e_sq) * N) * sin_lambda;

        double xd, yd, zd;
        xd = x - x0;
        yd = y - y0;
        zd = z - z0;

        // This is the matrix multiplication
        xEast = -sin_phi * xd + cos_phi * yd;
        yNorth = -cos_phi * sin_lambda * xd - sin_lambda * sin_phi * yd + cos_lambda * zd;
        zUp = cos_lambda * cos_phi * xd + cos_lambda * sin_phi * yd + sin_lambda * zd;
    }

    // Inverse of EcefToEnu. Converts East-North-Up coordinates (xEast, yNorth, zUp) in a
    // Local Tangent Plane that is centered at the (WGS-84) Geodetic point (lat0, lon0, h0)
    // to the Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z).
    public static void EnuToEcef(double xEast, double yNorth, double zUp,
                                    double lat0, double lon0, double h0,
                                    out double x, out double y, out double z)
    {
        // Convert to radians in notation consistent with the paper:
        var lambda = DegreesToRadians(lat0);
        var phi = DegreesToRadians(lon0);
        var s = Sin(lambda);
        var N = a / Sqrt(1 - e_sq * s * s);

        var sin_lambda = Sin(lambda);
        var cos_lambda = Cos(lambda);
        var cos_phi = Cos(phi);
        var sin_phi = Sin(phi);

        double x0 = (h0 + N) * cos_lambda * cos_phi;
        double y0 = (h0 + N) * cos_lambda * sin_phi;
        double z0 = (h0 + (1 - e_sq) * N) * sin_lambda;

        double xd = -sin_phi * xEast - cos_phi * sin_lambda * yNorth + cos_lambda * cos_phi * zUp;
        double yd = cos_phi * xEast - sin_lambda * sin_phi * yNorth + cos_lambda * sin_phi * zUp;
        double zd = cos_lambda * yNorth + sin_lambda * zUp;

        x = xd + x0;
        y = yd + y0;
        z = zd + z0;
    }

    // Converts the geodetic WGS-84 coordinated (lat, lon, h) to 
    // East-North-Up coordinates in a Local Tangent Plane that is centered at the 
    // (WGS-84) Geodetic point (lat0, lon0, h0).
    public static void GeodeticToEnu(double lat, double lon, double h,
                                        double lat0, double lon0, double h0,
                                        out double xEast, out double yNorth, out double zUp)
    {
        GeodeticToEcef(lat, lon, h, out double x, out double y, out double z);
        EcefToEnu(x, y, z, lat0, lon0, h0, out xEast, out yNorth, out zUp);
    }

    // Converts East-North-Up coordinates in a Local Tangent Plane that is centered at the 
    // (WGS-84) Geodetic point (lat0, lon0, h0) to the geodetic WGS-84 coordinated (lat, lon, h)
    public static void EnuToGeodetic(double xEast, double yNorth, double zUp,
                                        double lat0, double lon0, double h0,
                                        out double lat, out double lon, out double h
                                        )
    {
        EnuToEcef(xEast, yNorth, zUp, lat0, lon0, h0, out double x, out double y, out double z);
        EcefToGeodetic(x, y, z, out lat, out lon, out h);
    }


    static double DegreesToRadians(double degrees)
    {
        return PI / 180.0 * degrees;
    }

    static double RadiansToDegrees(double radians)
    {
        return 180.0 / PI * radians;
    }
}
