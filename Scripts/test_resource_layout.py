"""Protect resource-type roots while keeping existing Resources keys independent."""

from pathlib import Path
import tempfile
import unittest
import validate_game_foundation_project as VALIDATOR


class ResourceLayoutTests(unittest.TestCase):
    """Exercise incompatible asset types and valid Sprite representations."""

    def validate(self, files):
        """Run the real resource gate against an isolated serialized-asset layout."""
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            for relative, content in files.items():
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content)
            report = VALIDATOR.ValidationReport()
            VALIDATOR.validate_resource_layout(root, report)
            return report.errors

    def test_sprite_image_and_native_sprite_are_allowed(self):
        """Sprite imports and Unity's serialized Sprite form both belong under Sprites."""
        self.assertEqual([], self.validate({
            "Assets/Sprites/Icon.png": "image",
            "Assets/Sprites/Icon.png.meta": "TextureImporter:\n  textureType: 8\n",
            "Assets/Sprites/Fragment.asset": "%YAML 1.1\n--- !u!213 &21300000\nSprite:\n",
        }))

    def test_normal_map_and_default_texture_are_rejected(self):
        """An image extension alone must not admit ordinary material textures."""
        for texture_type in (0, 1):
            with self.subTest(texture_type=texture_type):
                self.assertEqual(1, len(self.validate({
                    "Assets/Sprites/Surface.png": "image",
                    "Assets/Sprites/Surface.png.meta": f"TextureImporter:\n  textureType: {texture_type}\n",
                })))

    def test_non_sprite_content_is_rejected(self):
        """Prefabs, materials, meshes, fonts, and mixed native assets cannot return to Sprites."""
        for name, content in {
            "Popup.prefab": "prefab", "Paint.mat": "material", "Model.fbx": "model", "Font.ttf": "font",
            "Mesh.asset": "--- !u!43 &4300000\nMesh:\n",
            "Mixed.asset": "--- !u!213 &21300000\nSprite:\n--- !u!114 &11400000\nMonoBehaviour:\n",
        }.items():
            with self.subTest(name=name):
                self.assertEqual(1, len(self.validate({"Assets/Sprites/" + name: content})))

    def test_typed_roots_do_not_change_resources(self):
        """Direct references and Resources may coexist without converting their loading mode."""
        self.assertEqual([], self.validate({
            "Assets/Prefabs/UI/Popup.prefab": "prefab",
            "Assets/Art/Materials/Surface.mat": "material",
            "Assets/Resources/Existing/Catalog.json": "{}",
            "Assets/Resources/Existing/Fonts/Font.ttf": "font",
        }))

    def test_duplicate_resources_keys_remain_blocking(self):
        """The new type checks must retain the original path-loading collision gate."""
        self.assertEqual(1, len(self.validate({
            "Assets/Resources/UI/Icon.png": "image",
            "Assets/Other/Resources/ui/icon.jpg": "image",
        })))

    def test_editor_subfolder_does_not_hide_wrong_type(self):
        """A folder named Editor cannot exempt non-Sprite assets from the Sprite-only root."""
        self.assertEqual(1, len(self.validate({"Assets/Sprites/Editor/Popup.prefab": "prefab"})))


if __name__ == "__main__":
    unittest.main()
