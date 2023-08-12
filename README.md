<h1 align="center">Manifoldsweeper</h1>

<h4 align="center">Minesweeper on two-dimensional manifolds</h4>

<p align="center">
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/cylinder_3D.png" alt="Cylinder" width="200" />
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/torus_3D.png" alt="Torus" width="200" />
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/Mobius_3D.png" alt="Möbius strip" width="200" />
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/Klein_3D.png" alt="Klein bottle" width="200" />
</p>

<p align="center">
  <a href="https://trestansimon.github.io/Manifoldsweeper/">Play the latest browser version here</a>
</p>


## Abstract

Manifoldsweeper is a variant of [Minesweeper](https://en.wikipedia.org/wiki/Minesweeper_(video_game)) that adapts the original game's gridded playing board to different surfaces while preserving the its existing rules. Such surfaces are constructed by gluing opposite edges of the game's grid in different configurations. These conditions allow for four different surfaces (up to certain deformations): the cylinder, torus, Möbius strip, and Klein bottle.

## Gameplay Guide

### Starting

1. Select a manifold—determines how the playing board will connect with/glue to itself (see [§ Manifolds](#manifolds))
3. Select a difficulty—determines the number of horizontal and vertical grid cells and the number of mines
   - `Easy`, `Medium`, or `Difficult` presets have fixed values that depend on the chosen manifold
   - `Custom` allows for custom values
4. Select a mapping—determines how the playing board will be presented (see [§ Mappings](#mappings))
6. Click "Generate"
7. Click anywhere on the surface to start a new game

### Playing

The rules of Manifoldsweeper are identical to that of ordinary Minesweeper (guides for which can be found [elsewhere](https://en.wikipedia.org/wiki/Minesweeper_(video_game)#Gameplay)).

Manifoldsweeper was originally designed to be played with a mouse and keyboard.

| Binding                           | Action               |
| --------------------------------- | -------------------- |
| Left mouse button                 | Reveal tile          |
| Right mouse button                | Flag tile            |
| Middle mouse button + move cursor | Rotate camera        |
| Scroll wheel                      | Zoom camera          |
| `esc` key                         | Open main menu/pause |
| `r` key                           | Reset game           |

### Mechanics

#### Manifolds
Each manifold can be constructed by gluing together the edges of an ordinary rectangular Minesweeper playing board in different configurations. These edges can be represented by pairs of colored arrows that are glued so that they lie on top of each other pointing in the same direction.

| Cylinder | Torus | Möbius strip | Klein bottle |
| :------: | :---: | :----------: | :----------: |
| <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Resources/Textures/UI/CylinderSquare.png" width="100"> | <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Resources/Textures/UI/TorusSquare.png" width="100"> | <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Resources/Textures/UI/MobiusSquare.png" width="100"> | <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Resources/Textures/UI/KleinSquare.png" width="100"> |

This "gluing" operation can be described more formally in terms of quotient spaces.
<details>
<summary>Further Information: Quotient Manifolds</summary>

The surfaces of Manifoldsweeper are the quotient manifolds of ordinary Minesweeper's two-dimensional Euclidean plane. In other words, each manifold is the quotient space of the unit square $I^2 / \sim$, where $\sim$ is the corresponding equivalence relation(s) from the table below.

| Manifold 		 | Equivalence relations                          		 |
| ------------ | --------------------------------------------------- |
| Cylinder 		 | $(0,\ t) \sim (1,\ t)$                         		 |
| Torus  		   | $(0,\ t) \sim (1,\ t)$ and $(s,\ 0) \sim (s,\ 1)$   |
| Möbius strip | $(0,\ t) \sim (1,\ 1-t)$                       		 |
| Klein bottle | $(0,\ t) \sim (1,\ 1-t)$ and $(s,\ 0) \sim (s,\ 1)$ |

These manifolds can be equivalently described as the quotients of the plane by certain wallpaper groups. (Further explanation pending.)
</details>

#### Mappings
There are different ways that two-dimensional manifolds can be represented in two- and three-dimensional space. Therefore, Manifoldsweeper allows the player to choose a particular mapping into these spaces when setting up a game and to re map any time after. There are two main categories of mappings to choose from: (1) those that map to a [universal covering space](https://en.wikipedia.org/wiki/Covering_space#Universal_covering) depicted in two-dimensional space and (2) those that [embed](https://en.wikipedia.org/wiki/Embedding) or [immerse](https://en.wikipedia.org/wiki/Immersion_(mathematics)) the manifold in three-dimensional space.

1. Each manifold has the "Flat" mapping chosen by default which represents the playing board as a flat rectangle, like original Minesweeper, but with the rectangle repeating infinitely at its glued edges if any are present. Any action completed on one tile will also be completed on all of its copies.

<p align="center">
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/Klein_flat.gif" width="600"/><br>
  <i>The "Flat" mapping of the Klein bottle</i>
</p>

2. All four manifolds have different embeddings or immersions in three-dimensional space that represent the playing board as a flat rectangle deformed such that its glued edges are attached to each other in the correct configuration.

<p align="center">
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/Klein_3D.png" width="600"/><br>
  <i>The "Bottle" immersion of the Klein bottle</i>
</p>

The manifold can be re mapped at any time by entering the main menu. Here, you can select a new map from the dropdown and click the "Map" button to re map.

<p align="center">
  <img src="https://media.githubusercontent.com/media/TrestanSimon/Manifoldsweeper/main/Assets/Samples/torus_map.gif" width="600"/><br>
  <i>Torus transitioning from the "Flat" map to the "Torus" map</i>
</p>


## Acknowledgements

Manifoldsweeper was developed as my senior project at Red Bank Regional High School's Academy of Information Technology.
