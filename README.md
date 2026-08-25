# Remote

Remote is an experimental project created to explore the architecture and communication flow behind a remote access system.

> **This is not a finished product.**

The purpose of this repository is to provide an initial insight into how a project of this kind can be designed and implemented.

At this stage, the project focuses primarily on concepts such as:

- Agent and Viewer communication
- Signaling
- WebSocket-based bidirectional communication
- Session creation and state management
- Message contracts shared between applications
- Basic separation of responsibilities between components

The current implementation should be considered a **starting point for experimentation and learning**, rather than a production-ready remote access solution.

Several aspects of a complete remote access system are either simplified, experimental, or not implemented yet.

## Project Structure

The solution is currently divided into components with different responsibilities:

- **Remote.Agent** — represents the machine that can receive a remote session.
- **Remote.Viewer** — represents the client requesting access to an Agent.
- **Remote.Signaling** — coordinates Agents, Viewers, and sessions.
- **Remote.Protocol** — contains the shared communication contracts and serialization logic.

## Current Goal

The current goal is to establish a clean communication flow between the components and understand the foundations required to build a remote access system.

The project will evolve incrementally as new concepts and technical challenges are explored.

## Disclaimer

This repository is intended for educational and experimental purposes.

The architecture, protocols, security mechanisms, performance characteristics, and implementation details may change significantly as the project evolves.

It should **not** be considered production-ready software.