# Crossdeck
A modern, cross-platform hardware and simulation bridge

## Development

This project is currently in development. It is open source and primarily developed for personal use but any contributions are welcome.

Having spent 10+ years developing web APIs and enterprise applications, this is my first time developing a hardware interface. Do not expect development to be full time. This is a hobby project.

## Roadmap

The roadmap is not fully thought out yet but here's what I'm thinking:

### Phase 1: Core and inputs *(in progress)*
Press a button on a HID device and something happens in the sim.

- [x] Solution structure
- [x] X-Plane connectivity through the Web API (12.1+)
- [x] HID device discovery, hot-plug, and a read loop for buttons, axes and hats
- [ ] Input dispatcher: hold commands (begin/end), released on unplug or sim change
- [ ] Profiles: hand-written JSON, versioned with `$schema`, picked by aircraft
- [ ] Stable device identity, so profiles survive a replug, reboot or port change
- [ ] Fault tolerance: supervised loops, reconnect to the sim, devices that fail to open don't stop the app

### Phase 2: Outputs
The sim drives the hardware.

- [ ] Sim values routed to LEDs, backlights and displays
- [ ] Output bindings in profiles

### Phase 3: Custom devices
Hardware that needs more than plain HID.

- [ ] JSON device definitions: named controls, encoders, switches and LED bit layouts
- [ ] Composite devices with several HID interfaces

### Phase 4: UI
A desktop app to set everything up without editing JSON.

... TBD ...

### Phase 5: Launch
The app is ready to be launched and distributed.

- [ ] Packaging for Windows, macOS and Linux
- [ ] Installer (VeloPack)
- [ ] Documentation (Technical, architecture, user guides)
- [ ] Release notes

# Contributing

While this is MIT licensed and you're free to fork, I would greatly appreciate if you would contribute to the development either by way of code, documentation or feedback/suggestions.

**Note**: I am not opposed to agentic development workflows. Claude is being used to define the architecture of this project which I am tweaking, refining and tailoring. However, it is important that the rationale behind any contribution can be explained by the author in their own words. Contributions should make sense and they should fit in with the application architecture.

# Structure

The structure is similar to the "Onion Architecture" which fellow enterprise developers may be familiar with. This architecture promotes separation of concerns and modularity, making it easier to maintain and extend the codebase. It also allows for better testability and reusability of components.

The added benefit is that the MSFS project can be conditionally compiled based on whether the OS is Windows, since all simulators are exposed behind a single interface with no tight coupling between contracts and implementations.

| Project | What lives there                                                                                                                                                                                         |
|---|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Crossdeck.Core` | Contracts only: interfaces, records, enums and value types. No package references. Every other project can use it.                                                                                       |
| `Crossdeck.Application` | The orchestration: device registry, sim session supervisor, input dispatcher, and later the value router, output writers and profile runtime. |
| `Crossdeck.Sim.XPlane` | X-Plane connectivity: detector, producer and factory. Models are generated from the X-Plane OpenAPI spec at build time (NSwag).                                                                          |
| `Crossdeck.Sim.MSFS` | MSFS through SimConnect, Windows only (`net10.0-windows`), referenced by the Host behind `#if WINDOWS`. Placeholder for now.                                                                             |
| `Crossdeck.Devices.Hid` | HID devices through HidSharp: watcher, driver factory, joystick driver, and the transport and report decoder (internal).                                                                                 |
| `Crossdeck.Host` | The composition root: DI registrations (`AddHostServices()`), configuration, and one hosted `SupervisorService<T>` for each `IRunnable`.                            |
| `Crossdeck.UI` | Avalonia desktop app (MVVM, CommunityToolkit) that starts the host. View models read state through Core interfaces and never touch drivers or producers directly.                                        |
