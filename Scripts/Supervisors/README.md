# What is a Supervisor?
A supervisor is a script that manages the game, it spawns players and manages the game state according to its rules.
Indeed, it defines the game rules and the game state.

## Basic Supervisor (made to be inherited)
It does nothing. But it has useful methods like SpawnPlayers for spawning players correctly.
### Authority
The authority is the first player that joined the game.
### Players spawning process
1. Prevent all behide objects from sending network messages (by removing them from the scene)
2. Each player spawns all player nodes with their synchronizer's visibility set to false.
3. Each player
   1. Wait every player to be ready (to have loaded the scene and to have spawned the players)
   2. Then set visible to them.
4. Allow behide objects to communicate

> ⚠️ Subtlety\
> We spawn the players that way to prevent MultiplayerSynchronizer errors.\
> See this related issues on
> [Godot's forum](https://forum.godotengine.org/t/multiplayersynchronizer-refusing-to-sync-node-not-found-on-valid-path/82944)
> or
> [GitHub](https://github.com/godotengine/godot/issues/91342)

## PropHunt Supervisor
### Rules
- The game starts with a 30 seconds countdown.
- The game ends after 5 minutes or when all the props are dead. <!-- TODO: Find the correct duration -->
- The game hunter is chosen randomly. (The choice is made by the authority)
