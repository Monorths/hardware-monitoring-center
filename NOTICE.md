# Third-Party Notices

This project uses the following third-party software at build or runtime.

## LibreHardwareMonitorLib

- Version: 0.9.6
- Project: https://github.com/LibreHardwareMonitor/LibreHardwareMonitor
- License: Mozilla Public License 2.0
- Use: Reads hardware sensors, including CPU, GPU, motherboard, memory and storage data.

The full MPL-2.0 text is included in `THIRD-PARTY-LICENSE-MPL-2.0.txt`.

## PawnIO

- Product: PawnIO Setup
- Version: 2.1.0
- Signer/company: namazso
- Use: Kernel driver component used by LibreHardwareMonitor for certain CPU and motherboard sensors.

The application obtains the signed setup executable from the official LibreHardwareMonitor repository during the build process. The expected SHA-256 is recorded in `scripts/fetch-deps.ps1`.

## Other LibreHardwareMonitor release dependencies

The official LibreHardwareMonitor release archive contains additional runtime libraries. They are downloaded from the official release during build and are not relicensed under this project's MIT license.

## Project ownership

The MIT license in `LICENSE` applies only to original source and documentation in this repository. Third-party names, binaries and licenses remain the property of their respective authors.
