using System.Collections.ObjectModel;
using System.Text.Json.Nodes;

namespace GLTFRuntime
{
    /// <summary>
    /// Contains information read from a gltf file.
    /// </summary>
    public sealed class glTF
    {
        /// <summary>
        /// A read-only collection of keyframe animations.
        /// </summary>
        public ReadOnlyCollection<Animation> Animations { get; }

        /// <summary>
        /// Metadata about the glTF asset.
        /// </summary>
        public Asset Asset { get; }

        /// <summary>
        /// A read-only collection of cameras.
        /// </summary>
        public ReadOnlyCollection<Camera> Cameras { get; }

        /// <summary>
        /// A read-only collection of images.
        /// </summary>
        public ReadOnlyCollection<Image>? Images { get; }

        /// <summary>
        /// A read-only collection of materials.
        /// </summary>
        public ReadOnlyCollection<Material> Materials { get; }

        /// <summary>
        /// A read-only collection of meshes.
        /// </summary>
        public ReadOnlyDictionary<string, Mesh> Meshes { get; }

        /// <summary>
        /// A read-only collection of nodes.
        /// </summary>
        public ReadOnlyCollection<Node> Nodes { get; }

        /// <summary>
        /// A read-only collection of samplers.
        /// </summary>
        public ReadOnlyCollection<TextureSampler> Samplers { get; }

        /// <summary>
        /// The default scene.
        /// </summary>
        public Scene? Scene { get; }

        /// <summary>
        /// A read-only collection of scenes.
        /// </summary>
        public ReadOnlyCollection<Scene> Scenes { get; }

        /// <summary>
        /// A read-only collection of skins.
        /// </summary>
        public ReadOnlyCollection<Skin>? Skins { get; }

        /// <summary>
        /// A read-only collection of textures.
        /// </summary>
        public ReadOnlyCollection<Texture> Textures { get; }

        /// <summary>
        /// Constructs a new <see cref="glTF"/> instance from the specified gltf file.
        /// </summary>
        /// <param name="binDirectory">The directory where bin files referred to in buffers can be found.</param>
        /// <param name="fileName">The name of the GLTF file to load.</param>
        public glTF(string binDirectory, string fileName)
        {
            JsonNode root = JsonNode.Parse(File.ReadAllText(Path.Combine(binDirectory, fileName)))!;

            Asset = new Asset(root["asset"]!); // Asset is required by the gltf spec

            JsonNode? camerasNode = root["cameras"];
            if (camerasNode == null)
                Cameras = new ReadOnlyCollection<Camera>(new List<Camera>());
            else
                Cameras = new ReadOnlyCollection<Camera>((from c in camerasNode.AsArray() select Camera.Create(c)).ToList());

            Buffer[] buffers = (from b in root["buffers"]!.AsArray() select new Buffer(b, binDirectory)).ToArray();
            BufferView[] bufferViews = (from bv in root["bufferViews"]!.AsArray() select new BufferView(bv, buffers)).ToArray();
            Accessor[] accessors = (from node in root["accessors"]!.AsArray() select new Accessor(node!, bufferViews)).ToArray();

            JsonNode? nodesNode = root["nodes"];
            if (nodesNode == null)
                Nodes = new ReadOnlyCollection<Node>(new List<Node>());
            else
            {
                var nodesArray = nodesNode!.AsArray();
                var nodes = (from n in nodesArray select new Node(n)).ToArray();
                for (int i = 0; i < nodesArray.Count; i++)
                {
                    var children = nodesNode[i]!["children"]?.AsArray();
                    if (children != null)
                    {
                        List<Node> childNodes = new List<Node>();
                        foreach (var childIndex in from c in children select c.GetValue<int>())
                        {
                            childNodes.Add(nodes[childIndex]);
                            nodes[childIndex].Parent = nodes[i];
                        }
                        nodes[i].Children = new ReadOnlyCollection<Node>(childNodes);
                    }
                }

                Nodes = new ReadOnlyCollection<Node>(nodes.ToList());
            }

            JsonArray? scenesNode = root["scenes"]?.AsArray();
            if (scenesNode == null)
                Scenes = new ReadOnlyCollection<Scene>(new List<Scene>());
            else
                Scenes = new((from s in scenesNode select new Scene(s, Nodes)).ToList());

            JsonNode? sceneNode = root["scene"];
            if (sceneNode == null)
                Scene = null;
            else
                Scene = Scenes[root["scene"]!.GetValue<int>()];

            var imagesNode = root["images"];
            if (imagesNode == null)
                Images = new ReadOnlyCollection<Image>(new List<Image>());
            else
                Images = new((from i in imagesNode.AsArray() select new Image(i, bufferViews, binDirectory)).ToList());

            var samplersNode = root["samplers"];
            if (samplersNode == null)
                Samplers = new ReadOnlyCollection<TextureSampler>(new List<TextureSampler>());
            else
                Samplers = new ReadOnlyCollection<TextureSampler>((from s in samplersNode.AsArray() select new TextureSampler(s)).ToList());

            var texturesNode = root["textures"];
            if (texturesNode == null)
                Textures = new ReadOnlyCollection<Texture>(new List<Texture>());
            else
                Textures = new ReadOnlyCollection<Texture>((from t in texturesNode.AsArray() select new Texture(t, Samplers!, Images!)).ToList());

            JsonNode materialsNode = root["materials"]!;
            if (materialsNode == null)
                Materials = new ReadOnlyCollection<Material>(new List<Material>());
            else
                Materials = new ReadOnlyCollection<Material>((from m in materialsNode.AsArray() select new Material(m, Textures)).ToList());

            JsonNode? meshesNode = root["meshes"];
            if (meshesNode == null)
                Meshes = new ReadOnlyDictionary<string, Mesh>(new Dictionary<string, Mesh>());
            else
                Meshes = new ReadOnlyDictionary<string, Mesh>(
                    (from m in meshesNode.AsArray() select new Mesh(m, accessors, Materials)).ToDictionary(m => m.Name)
                    );

            JsonNode? skinsNode = root["skins"];
            if (skinsNode == null)
                Skins = new ReadOnlyCollection<Skin>(new List<Skin>());
            else
                Skins = new ReadOnlyCollection<Skin>((from s in skinsNode.AsArray() select new Skin(s, Nodes, accessors)).ToList());

            var animationsNode = root["animations"];
            if (animationsNode == null)
                Animations = new ReadOnlyCollection<Animation>(new List<Animation>());
            else
                Animations = new ReadOnlyCollection<Animation>((from a in animationsNode.AsArray() select new Animation(a, Nodes, accessors)).ToList());
        }
    }
}
