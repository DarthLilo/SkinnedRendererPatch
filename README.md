# SkinnedRendererPatch

Patches the game variant system to support SkinnedMeshRenderers and allows states such as random materials or meshes to be saved across sessions!

This mods effects are barely noticible on its own, at most you'll notice flasks having their different shapes retained, this mod is best used as a library for your own scrap mods.

## How?

When creating an item in unity, there are two fields called "Mesh Variants" and "Material Variants" each doing exactly what their name says.

However they don't work with SkinnedMeshRenderers by default and only with MeshRenderers, with this mod installed you can now use these fields with SkinnedMeshRenderers.

![Mesh/Material Variants](https://i.imgur.com/T9DkTg3.png)

There are a few important things to note.
- Due to the nature of SkinnedMeshRenderers possibly existing multiple times underneath one model, you must join all of your meshes into one renderer **IF** using the mesh variants. (Material variants aren't affected by this)

- You will not see the effects of either if you spawn them in using [Imperium](https://thunderstore.io/c/lethal-company/p/giosuel/Imperium/) or Lethal Company's built in development menu, the code is only ran when landing on a moon!

- This mod is required on ALL clients, values will not sync otherwise and if a mod is attempting to replace a mesh on a SkinnedMeshRenderer without this mod it will cause all scrap to be worth nothing on that client.