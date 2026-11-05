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