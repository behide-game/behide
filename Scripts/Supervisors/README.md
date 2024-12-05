# What is a Supervisor?
A supervisor is a script that manages the game, it spawns players and manages the game state according to its rules.
So defines the game rules and the game state.

## PropHunt Supervisor
### Rules
- The game starts with a 30 seconds countdown.
- The game ends after 5 minutes or when all the props are dead. <!-- TODO: Find the correct duration -->
- The game hunter is chosen randomly. (The choice is made by the last player to join the room)

### Players spawning process
- All the players are spawned with their synchronizers's visibility set to false directly when the scene is loaded.
- We wait all the players to be ready, to have the game and to have spawned the players.
- We set the synchronizer's visibility for our player to true.

> ⚠️ Subtlety\
> We spawn the players with their visibility set to false to avoid bugs with the MultiplayerSynchronizers.
> See [this issue on Godot's forum](https://forum.godotengine.org/t/multiplayersynchronizer-refusing-to-sync-node-not-found-on-valid-path/82944)
