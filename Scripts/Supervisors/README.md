# What is a Supervisor?
A supervisor is a script that manages the game, it spawns players and manages the game state according to its rules.
Indeed, it defines the game rules and the game state.

# Base Supervisor (intended to be inherited)
The `Supervisor` class doesn't contain game-specific logic.
It only provides utility methods and features such as hiding the behide objects on the authority to prevent bugs.

## Authority
The **authority** is the first player to have joined the game.\
This peer is responsible for managing certain tasks like spawning players or distributing the roles.

## Players spawning process
To avoid [`MultiplayerSynchronizer`](https://docs.godotengine.org/en/stable/classes/class_multiplayersynchronizer.html) errors
when spawning a new player, the `BasicSupervisor` coordinates the spawn using the following steps:
1. **Each peer instantiates the player node locally**.\
    This ensures the node exists on all peers before synchronization begins.
2. **On the authoritative peer for that player node**
   1. It waits until the **non-authoritative peers** have finished spawning the player node.
   2. Once a peer is ready, it **enables visibility** on the `MultiplayerSynchronizer`, allowing it the synchronization.

> 💡️ Note: The visibility is intentionally disabled at first to prevent synchronization errors
> when peers receive updates for nodes that haven't been added to their scene yet.

> ⚠️ Warning: Spawning the same player **multiple times** can lead to unexpected behavior or bugs.

> 🪲️ Godot Quirk/Bug:
> This spawning process is necessary due to limitations in
> [`MultiplayerSynchronizer`](https://docs.godotengine.org/en/stable/classes/class_multiplayersynchronizer.html),
> which can throw errors like “Node not found on valid path” if synchronization starts
> before the node is added to the scene tree of a remote peer.
> Here is a similar issue on this [Godot forum discussion](https://forum.godotengine.org/t/multiplayersynchronizer-refusing-to-sync-node-not-found-on-valid-path/82944)

## Behide objects spawning process
To avoid [`MultiplayerSynchronizer`](https://docs.godotengine.org/en/stable/classes/class_multiplayersynchronizer.html) errors
when loading the game scene, the authoritative peer **removes all the behide objects from its scene** during initial loading.
Once all peers have fully loaded the scene, those objects are added back.
