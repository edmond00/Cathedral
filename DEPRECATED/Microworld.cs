using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using System.Linq;
using System.Collections.Generic;
using World;
using UnityEditor;
using Unity.Profiling;
using System;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine.Profiling;



namespace Microworld {

    public class AsciiWorldRender : World.Render {
        public float radius = 25;
        public int divisions = 5;
        // World.Sphere core;
        public World.TextSphere sphere;
        public IcoSphere.Geometry Mesh => sphere.mesh;
        public AsciiWorldRender(AsciiWorld world) : base(new MaterialDatabase.Color(255, 255, 255, 255, MaterialDatabase.Type.NORMAL, MaterialDatabase.Order.NORMAL), world) {
            // core = new World.Sphere(100, 20f, Color(30, 30, 30));
            sphere = new World.TextSphere(radius, divisions);
        }
    }

    public class AsciiWorld : World.ProceduralRenderer {
        public AsciiWorldRender render;
        public IcoSphere.Geometry mesh => render.Mesh;
        protected override void Setup(World.ObjectState state) {
        }
        protected override void MakeRender() {
            render = new AsciiWorldRender(this);
        }
        protected override void SetGlobalAccess() {
            Global.asciiworld = this;
        }
    }
    public class WorldGraph : World.AbstractPathGraph {

        IcoSphere.Geometry mesh => Global.asciiworld.mesh;
        Dictionary<int, HashSet<int>> links = new Dictionary<int, HashSet<int>>();

        public override Vector3 Position(int node) {
            return mesh.Positions[node];
        }
        public override IEnumerable<int> LinkedTo(int node) {
            return links[node];
        }
        protected override IEnumerable<int> GetNodesOf(ulong objId) {
            return Enumerable.Empty<int>();
        }
        public override bool Contains(int node) {
            return node >= 0 && node < mesh.Positions.Count;
        }

        public float CostNoise() {
            return Utils.RandomFloat() / Global.asciiworld.render.radius;
        }
        public override float Cost(int nodeA, int nodeB) {
            return Geometry.Distance(mesh.Positions[nodeA], mesh.Positions[nodeB]) + CostNoise();
        }

        private void AddLink(int nodeA, int nodeB) {
            if (links.ContainsKey(nodeA) == false) {
                links.Add(nodeA, new HashSet<int>());
            }
            if (links.ContainsKey(nodeB) == false) {
                links.Add(nodeB, new HashSet<int>());
            }
            links[nodeA].Add(nodeB);
            links[nodeB].Add(nodeA);
        }
        protected override void Setup(World.ObjectState state) {
            foreach (IcoSphere.TriangleIndices triangle in mesh.TriangleIndices) {
                AddLink(triangle.v1, triangle.v2);
                AddLink(triangle.v1, triangle.v3);
                AddLink(triangle.v3, triangle.v2);
            }
        }
    }

    public interface EventInfo {

    }

    public class Message {
        int updateSpeed = 10;
        int cursor = 0;
        string text = "";
        public bool displaying = false;

        public bool startGenerating => text.Length > 0;

        static string RemoveSubstringAndAfter(string large, string small)
        {
            if (string.IsNullOrEmpty(large) || string.IsNullOrEmpty(small)) return large;

            int index = large.IndexOf(small, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return large.Substring(0, index);
            }
            return large; // Return original if smallString is not found
        }
        static string TrimEnd(string large, string small)
        {
            if (string.IsNullOrEmpty(large) || string.IsNullOrEmpty(small)) return large;
            for (int i = 1; i < small.Length; i++) {
                if (large.EndsWith(small.Substring(0, i), StringComparison.OrdinalIgnoreCase)) {
                    return large.Substring(0, large.Length - 1);
                }
            }
            return large;
        }

        public void ReceiveText(string str) {
            if (pending)
            {
                return;
            }
            Debug.Log("RECEIVE LLM : " + str);
            text = str.Replace("’", "'").Replace("—", "-").Replace("è", "e");
            text = Regex.Replace(text, @"[^\x00-\x7F]", "'");

            text = RemoveSubstringAndAfter(text, "(note:");
            text = TrimEnd(text, "(note:");

            text = RemoveSubstringAndAfter(text, ". (");
            text = TrimEnd(text, ". (");
            
        }
        public void Cancel()
        {
            Debug.Log("CANCEL LLM");
            if (running && !canceling) Service.microworldNarrator.Cancel();
        }
        public void Complete()
        {

        }
        public bool canceling => Service.microworldNarrator.canceling;
        public bool running => Service.microworldNarrator.running;
        public bool pending => Service.microworldNarrator.pending;
        public void Write(EventInfo eventInfo) {
            // Assert.IsTrue(end);
            Cancel();
            displaying = true;
            cursor = 0;
            text = "";
            Service.microworldUI.narration.counter = 0;
            Service.microworldNarrator.Describe(
                eventInfo,
                ReceiveText,
                Complete
            );
        }
        public string VisibleText() {
            if (pending)
            {
                cursor = 0;
                return "";
            }
            if (text.Length == 0) return "";
            return text.Substring(0, Math.Min(cursor, text.Length));
        }

        public void Skip() {
            displaying = false;
            cursor = text.Length;
        }

        public void Update() {
            if (cursor < text.Length) {
                Debug.Log(text);
                if (displaying) Service.audio.Play("write");
                cursor += updateSpeed;
                if (cursor > text.Length) cursor = text.Length;
            }
        }
    }


    public class Microworld : Service, World.PathFinderClient
    {

        // Config
        public static int actionNameMaxLength => (int)(Config.terminalWidth * 0.9);
        public static int YnarrationUI = 3;
        public static int HnarrationUI = 6;
        public static float relevancyDifficulty = 0.66f;
        public static float expertiseDifficulty = 0.33f;

        public RunRDB runRDB;
        public WorldRDB worldRDB;
        public List<CompanionRDB> companionsRDB = new List<CompanionRDB>();

        public enum Step
        {
            CHARACTER_CREATION,
            LOADING,
            FIRST_MSG,
            CHOOSE_DESTINATION,
            START_TRAVEL_MSG,
            TRAVEL,
            CHOOSE_NEXT_ACTION,
            EVENT_NARRATION,
            CHOOSE_SKILLS,
            ROLL_DICE,
            GREEN_NUMBER_ROLL_DICE,
            ACTION_SKILL_CHECK_POPUP,
            CONSUMPTION_POPUP,
            REMOVE_SKILL_AFTER_ACTION,
            REMOVE_COMPANION_AFTER_ACTION,
            REMOVE_IDEA_AFTER_ACTION,
            OLD_AGE_MSG,
            CORRUPTION_MSG,
            ADDICTION_MSG,
            DEATH_MSG,
            USE_GREEN_NUMBER,
            BURN_VITAL_BILE
        }
        public Step currentStep = Step.CHARACTER_CREATION;
        float camHeight = 150f;
        public Avatar avatar;
        AsciiWorld world;
        WorldGraph graph;
        public GameObject cam;
        public IcoSphere.Geometry mesh => world.mesh;

        public Message message = new Message();

        protected override void LinkService()
        {
            Service.microworld = this;
        }

        public float radius => mesh.radius * 0.75f;
        public float camRadius => radius / 6f;
        public float mouseRadiusMax => radius;
        public float mouseRadius => camRadius + (radius - camRadius) * avatar.Youngness;
        public float nearRadius => mouseRadiusMax / 3f;
        public float Distance(int a, int b) => Geometry.Distance(mesh.Positions[a], mesh.Positions[b]);


        public NarrationCursor narrationCursor = new NarrationCursor();
        public IAction currentAction = null;
        public bool lastSkillCheckSuccessUseGreenNumber = false;
        public bool lastSkillCheckSuccess = true;
        public Accident currentAccident = null;
        public int currentPosition = 0;
        public bool reclick = false;
        public Cell currentCell => Info.cells[cells[currentPosition]];
        public int focusPosition = 0;
        public int targetPosition = -1;
        public Cell targetCell => Info.cells[cells[targetPosition]];

        public List<int> walk = null;

        public World.Path currentPath => GetCurrentPath();
        Dictionary<int, World.Path> _paths = null;
        public SkillChecker skillChecker = null;

        List<int> randomNumbers = new List<int>();
        public HashSet<int> modifiedCells = new HashSet<int>();
        public Utils.HookableDictionary<int, string> cells = new Utils.HookableDictionary<int, string>();
        public Dictionary<int, string> biomeCells = new Dictionary<int, string>();
        public Dictionary<string, List<int>> biomes = new Dictionary<string, List<int>>();
        public Dictionary<int, string> locations = new Dictionary<int, string>();
        public Dictionary<int, Action> willAnchors = new Dictionary<int, Action>();
        public Dictionary<int, Action> requirementAnchors = new Dictionary<int, Action>();
        public bool clickInGreenMode = false;

        public int maxWaypoints = 3;
        public List<int> waypoints = new List<int>();

        public Action TargetWill()
        {
            if (avatar.corrupted && cells.ContainsKey(targetPosition) && cells[targetPosition] == SPECIAL.cureLocation)
            {
                return null;
            }
            if ((clickInGreenMode || greenMode) && softAnchorsCache != null && softAnchorsCache.ContainsKey(targetPosition))
            {
                return softAnchorsCache[targetPosition];

            }
            if (hardAnchorsCache != null && hardAnchorsCache.ContainsKey(targetPosition))
            {
                return hardAnchorsCache[targetPosition];

            }
            return null;
        }
        public bool IsAtTarget()
        {
            return currentPosition == targetPosition && !Walking();
        }


        public Step NextStep() => currentStep switch
        {
            Step.CHARACTER_CREATION => Step.LOADING,
            Step.LOADING => (this.runRDB.in_progress.Value == 0 ? Step.FIRST_MSG : Step.CHOOSE_DESTINATION),
            Step.FIRST_MSG => Step.CHOOSE_DESTINATION,
            Step.CHOOSE_DESTINATION => Step.START_TRAVEL_MSG,
            Step.START_TRAVEL_MSG => Step.TRAVEL,
            Step.TRAVEL => Step.EVENT_NARRATION,
            Step.EVENT_NARRATION => StepAfterNarrationEvent(),
            Step.CHOOSE_NEXT_ACTION => (currentAction.Difficulty() >= 0 ? Step.CHOOSE_SKILLS : ActionConsequence()),
            Step.CHOOSE_SKILLS => (lastChosenSkills == null || lastChosenSkills.Count == 0) ? ActionConsequence() : Step.ROLL_DICE,
            Step.ROLL_DICE => (CanUseGreenNumber() ? Step.USE_GREEN_NUMBER : (CanBurnVitalBile() ? Step.BURN_VITAL_BILE : ActionConsequence())),
            Step.GREEN_NUMBER_ROLL_DICE => (CanBurnVitalBile() ? Step.BURN_VITAL_BILE : ActionConsequence()),
            Step.BURN_VITAL_BILE => ActionConsequence(),
            Step.USE_GREEN_NUMBER => (CanBurnVitalBile() ? Step.BURN_VITAL_BILE : ActionConsequence()),
            Step.ACTION_SKILL_CHECK_POPUP => FinalizeActionStep(),
            Step.CONSUMPTION_POPUP => FinalizeActionStep(),
            Step.REMOVE_SKILL_AFTER_ACTION => FinalizeActionStep(),
            Step.REMOVE_COMPANION_AFTER_ACTION => FinalizeActionStep(),
            Step.REMOVE_IDEA_AFTER_ACTION => FinalizeActionStep(),
            Step.OLD_AGE_MSG => LoopBackStep(false, true, true),
            Step.CORRUPTION_MSG => LoopBackStep(false, false, true),
            Step.ADDICTION_MSG => LoopBackStep(false, false, false),
        };

        bool CanBurnVitalBile()
        {
            if (lastSkillCheckSuccess)
            {
                return false;
            }
            if (avatar.canBurnVitalBile == false)
            {
                return false;
            }
            if (avatar.health <= 1)
            {
                return false;
            }
            return true;
        }
        bool CanUseGreenNumber()
        {
            if (lastSkillCheckSuccess)
            {
                return false;
            }
            return (greenMode && avatar.greenNumber < 100);
        }

        World.Path currentPathCache = null;
        bool needNewPath = true;
        World.Path GetCurrentPath()
        {
            if (currentPathCache != null && !needNewPath) return currentPathCache;
            if (_paths == null || _paths.Count == 0) return currentPathCache;
            List<Vector3> positions = new List<Vector3>();
            List<int> nodes = new List<int>();
            float cost = 0f;

            var paths = new Dictionary<int, World.Path>(_paths);

            int tmp = currentPosition;
            while (tmp != targetPosition)
            {
                bool found = false;
                if (paths.Count == 0) return currentPathCache;
                if (paths.ContainsKey(tmp))
                {
                    var path = paths[tmp];
                    int todel = tmp;
                    if (path.nodes != null && path.nodes.Count > 0 && path.nodes[0] == tmp)
                    {
                        for (int i = 0; i < path.Length(); i++)
                        {
                            if (tmp == path.nodes[i] && tmp != currentPosition) continue;
                            positions.Add(path.positions[i]);
                            nodes.Add(path.nodes[i]);
                            tmp = path.nodes[i];
                        }
                        found = true;
                    }
                    paths.Remove(todel);
                }
                if (!found)
                {
                    return currentPathCache;
                }
            }
            needNewPath = false;
            currentPathCache = new World.Path(positions, nodes, cost);
            return currentPathCache;
        }


        void ResetPaths()
        {
            needNewPath = true;
            var oldPaths = new Dictionary<int, World.Path>();
            if (_paths != null)
            {
                oldPaths = new Dictionary<int, World.Path>(_paths);
            }
            _paths = new Dictionary<int, World.Path>();
            void RequestPath(int p1, int p2)
            {
                if (oldPaths.ContainsKey(p1) && oldPaths[p1].nodes.Last() == p2)
                {
                    _paths.Add(p1, oldPaths[p1]);
                }
                else
                {
                    Service.pathFinderServer.Request(p1, p2, graph, this);
                }
            }
            if (waypoints == null || waypoints.Count == 0)
            {
                RequestPath(currentPosition, targetPosition);
                return;
            }
            int tmp = currentPosition;
            foreach (int wp in waypoints)
            {
                RequestPath(tmp, wp);
                tmp = wp;
            }
            RequestPath(tmp, targetPosition);
        }

        public void AddWaypoint()
        {
            if (targetPosition < 0) return;
            if (waypoints.Contains(targetPosition))
            {
                waypoints.Remove(targetPosition);
            }
            else
            {
                waypoints.Add(targetPosition);
                if (waypoints.Count > maxWaypoints)
                {
                    waypoints = waypoints.Skip(waypoints.Count - maxWaypoints).ToList();
                }
            }
            ResetPaths();
        }

        public void BurnVitalBile()
        {
            skillChecker.Reset();
            avatar.health -= 1;
            Service.microworld.StartStep(Step.ROLL_DICE);
        }

        public void UseGreenNumber()
        {
            RoleGreenNumber();
            Service.microworld.StartStep(Step.GREEN_NUMBER_ROLL_DICE);
        }

        bool ContinueNewLocation()
        {
            if (lastSkillCheckSuccess == false) return false;
            if (lastNewLocationCell == currentPosition &&
                currentAction.new_location_type == SPECIAL.NewLocationTypes.HERE &&
                currentCell is Location &&
                (currentCell as Location).continue_narration == true)
            {
                return true;
            }
            return false;
        }

        Step NextNarrationStep()
        {
            if (soakUpItem != null) return Step.CHOOSE_DESTINATION;
            if (narrationCursor.last || lastSkillCheckSuccess == false)
            {
                if (ContinueNewLocation())
                {
                    if (Service.microworldUI.iconRawImage != null) Service.microworldUI.iconRawImage.texture = CellIcon(currentPosition);
                    narrationCursor.SetCurrentNode(GetEntryNode(true));
                    return Step.EVENT_NARRATION;

                }
                lastSkillCheckSuccessUseGreenNumber = false;
                lastSkillCheckSuccess = true;
                narrationCursor = new NarrationCursor(); // RESET
                return LoopBackStep();
            }
            if (narrationCursor.nextChoices != null && narrationCursor.nextChoices.Count >= 1)
            {
                return Step.CHOOSE_NEXT_ACTION;
            }
            Assert.IsNotNull(narrationCursor.nextEvent);
            narrationCursor.SetCurrentNode(narrationCursor.nextEvent.node);
            return Step.EVENT_NARRATION;

        }
        Step StepAfterNarrationEvent()
        {
            if (soakUpItem != null)
            {
                return Step.CONSUMPTION_POPUP;
            }
            if (needActionPopUp)
            {
                needActionPopUp = false;
                return Step.ACTION_SKILL_CHECK_POPUP;
            }
            else
            {
                return NextNarrationStep();
            }
        }

        bool needActionPopUp = false;
        Step ActionConsequence()
        {
            needActionPopUp = true;
            UpdateAvatar();
            return Step.EVENT_NARRATION;
        }
        Step LoopBackStep(bool checkAge = true, bool checkCorruption = true, bool checkAddiction = true)
        {
            currentAccident = null;
            if (checkAge && avatar.OldAgeDamage())
            {
                return Step.OLD_AGE_MSG;
            }
            if (checkCorruption && avatar.corrupted)
            {
                return Step.CORRUPTION_MSG;
            }
            if (checkAddiction && avatar.imbuement.addiction != null && avatar.imbuement.addiction.Length > 0)
            {
                if (avatar.imbuement.item == null || avatar.imbuement.item.addiction != avatar.imbuement.addiction)
                {
                    return Step.ADDICTION_MSG;
                }
            }
            if (Dead()) return Step.DEATH_MSG;
            if (walk != null && walk.Count > 0)
            {
                return Step.TRAVEL;
            }
            return Step.CHOOSE_DESTINATION;
        }

        Step FinalizeActionStep()
        {
            if (avatar.SkillOverMemoryCount() > 0) return Step.REMOVE_SKILL_AFTER_ACTION;
            if (avatar.CompanionOverLimitCount() > 0) return Step.REMOVE_COMPANION_AFTER_ACTION;
            if (avatar.IdeaOverLimitCount() > 0 || avatar.hadNewIdea)
            {
                avatar.hadNewIdea = false;
                return Step.REMOVE_IDEA_AFTER_ACTION;
            }
            return NextNarrationStep();
        }

        public Cell ActionNewLocation()
        {
            if (currentAction.new_location != null && currentAction.new_location.Length > 0)
            {
                return Info.cells[currentAction.new_location];
            }
            return null;
        }
        public Cell FailureReplacementLocation()
        {
            if (currentAction.failure_location_replacement != null && currentAction.failure_location_replacement.Length > 0)
            {
                return Info.cells[currentAction.failure_location_replacement];
            }
            return null;
        }
        public int lastNewLocationCell = -1;

        public bool Dead() => (avatar.health <= 0);

        public int currentStepCounter = 0;
        public void EndStep(Step step)
        {
            switch (step)
            {
                case Step.ROLL_DICE:
                    {
                        if (lastSkillCheckSuccess)
                        {
                            Service.audio.Play("success");
                        }
                        else
                        {
                            Service.audio.Play("fail");
                        }
                        break;
                    }
                case Step.CHARACTER_CREATION:
                    {
                        avatar.Reset();
                        break;
                    }
                case Step.ACTION_SKILL_CHECK_POPUP:
                    {
                        if (lastNewLocationCell >= 0)
                        {
                            FocusOn(currentPosition);
                        }
                        Service.microworldUI.choicesUI.CleanSkillUsed();
                        break;
                    }
                case Step.EVENT_NARRATION:
                    {
                        lastChosenSkills = null;
                        break;
                    }
                case Step.CHOOSE_DESTINATION:
                    {
                        currentPathCache = null;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        public void ResetRDB(bool resetWorld)
        {
            if (resetWorld)
            {
                worldRDB.Zero();
            }
            runRDB.Zero();
            foreach (var crdb in companionsRDB)
            {
                crdb.Zero();
            }
        }

        public int epoch = 0;
        public void StartStep(Step step)
        {
            Debug.Log("start step");
            Debug.Log(currentStep.ToString());
            Debug.Log(step.ToString());
            EndStep(currentStep);
            currentStepCounter = 0;
            var previousStep = currentStep;
            currentStep = step;
            switch (step)
            {
                case Step.LOADING:
                    {
                        epoch = 0;
                        break;
                    }
                case Step.DEATH_MSG:
                    {
                        epoch = 0;
                        runRDB.in_progress.Value = 0;
                        break;
                    }
                case Step.CHARACTER_CREATION:
                    {
                        epoch = 0;
                        runRDB.in_progress.Value = 0;
                        ResetRDB(true);
                        break;
                    }
                case Step.CHOOSE_DESTINATION:
                    {
                        epoch += 1;
                        Profiler.BeginSample("cancel narration");
                        message.Cancel();
                        clickInGreenMode = false;
                        runRDB.in_progress.Value = 1;
                        Profiler.EndSample();

                        Profiler.BeginSample("save run");
                        SaveRun();
                        Profiler.EndSample();

                        Profiler.BeginSample("reset display");
                        hideUI = false;
                        lastNewLocationCell = -1;
                        soakUpItem = null;
                        if (Service.microworldUI.iconRawImage != null)
                        {
                            Service.microworldUI.iconRawImage.texture = CellIcon(currentPosition);
                        }
                        FocusOn(currentPosition);
                        waypoints = new List<int>();
                        _paths = null;
                        if (!avatar.Emprisoned())
                        {
                            Service.audio.Play("success");
                            Service.audio.Play("accident");
                        }
                        Profiler.EndSample();

                        Profiler.BeginSample("update anchors");
                        UpdateAnchors();
                        Profiler.EndSample();

                        Profiler.BeginSample("other");
                        Service.microworldNarrator.Reset();
                        avatar.imbuement.Update();
                        if (avatar.Emprisoned())
                        {
                            targetPosition = currentPosition;
                            Service.microworld.clickInGreenMode = false;
                            Service.microworld.StartStep(Service.microworld.NextStep());
                        }
                        Profiler.EndSample();

                        break;
                    }
                case Step.FIRST_MSG:
                    {
                        MoveTo(currentPosition);
                        Service.microworldNarrator.Reset();
                        Service.audio.Play("success");
                        Service.audio.Play("accident");
                        message.Write(new FirstMessage());
                        break;
                    }
                case Step.ROLL_DICE:
                    {
                        Service.audio.Play("step");
                        break;
                    }
                case Step.CHOOSE_SKILLS:
                    {
                        Service.audio.Play("step");
                        Service.microworldUI.choicesUI.SetAction(currentAction);
                        break;
                    }
                case Step.REMOVE_COMPANION_AFTER_ACTION:
                    {
                        Service.audio.Play("step");
                        Service.microworldUI.removeCompanionUI.Activate();
                        break;
                    }
                case Step.REMOVE_IDEA_AFTER_ACTION:
                    {
                        Service.audio.Play("step");
                        Service.microworldUI.removeIdeaUI.Activate();
                        break;
                    }
                case Step.REMOVE_SKILL_AFTER_ACTION:
                    {
                        Service.audio.Play("step");
                        Service.microworldUI.removeSkillUI.Activate();
                        break;
                    }
                case Step.START_TRAVEL_MSG:
                    {
                        Service.audio.Play("step");
                        FocusOn(focusPosition);
                        if (avatar.Emprisoned())
                        {
                            walk = new List<int>();
                            targetPosition = currentPosition;
                            StartStep(NextStep());
                        }
                        else if (targetPosition == currentPosition)
                        {
                            walk = new List<int>();
                            reclick = true;
                            StartStep(NextStep());
                        }
                        else
                        {
                            // TODO fix NullReferenceException
                            if (currentPath == null || currentPath.nodes == null)
                            {
                                StartStep(Step.CHOOSE_DESTINATION);
                            }
                            else
                            {
                                walk = currentPath.nodes.Clone();
                                reclick = false;
                                message.Write(new TravelTowardMessage());
                            }
                        }
                        break;
                    }
                case Step.TRAVEL:
                    {
                        message.Cancel();
                        Service.audio.Play("step");
                        break;
                    }
                case Step.CHOOSE_NEXT_ACTION:
                    {
                        if (narrationCursor.nextChoices != null && narrationCursor.nextChoices.Count == 1)
                        {
                            NodeChoiceSelection(0);
                        }
                        else
                        {
                            Service.audio.Play("step");
                        }
                        break;
                    }
                case Step.EVENT_NARRATION:
                    {
                        if (currentAccident != null && previousStep == Step.TRAVEL)
                        {
                            Service.audio.Play("accident");
                        }
                        else
                        {
                            Service.audio.Play("step");
                        }
                        message.Write(narrationCursor.Info());
                        break;
                    }
                case Step.OLD_AGE_MSG:
                    {
                        Service.audio.Play("accident");
                        avatar.health -= 1;
                        break;
                    }
                case Step.CORRUPTION_MSG:
                    {
                        if (Utils.RandomFloat() <= Service.microworld.avatar.humorCorruptionResistance)
                        {
                            StartStep(NextStep());
                        }
                        else
                        {
                            Service.audio.Play("accident");
                            avatar.health -= 1;
                        }
                        break;
                    }
                case Step.ADDICTION_MSG:
                    {
                        if (Utils.RandomFloat() <= Service.microworld.avatar.addictionResistance)
                        {
                            StartStep(NextStep());
                        }
                        else
                        {
                            Service.audio.Play("accident");
                            avatar.health -= 1;
                        }
                        break;
                    }
                case Step.ACTION_SKILL_CHECK_POPUP:
                    {
                        Service.audio.Play("step");
                        break;
                    }
                default:
                    {
                        Service.audio.Play("step");
                        break;
                    }
            }
        }

        public int fullfilledGreenNumber = -1;
        public void NodeChoiceSelection(int index)
        {
            Assert.IsNotNull(narrationCursor.nextChoices);
            var choice = narrationCursor.nextChoices[index];
            currentAction = choice.action;
            fullfilledGreenNumber = -1;
            if (TargetWill() != null && currentAction.WillName() == TargetWill().WillName())
            {
                fullfilledGreenNumber = CellGreenNumber(currentPosition);
                Debug.Log("DBG fullfill green number");
                RemoveAnchor(targetPosition);
            }
            choice.node.SetChosenAction(choice.action);
            if (choice.action.DilemmaFollow())
            {
                var nextNode = new VirtualChoice(choice.action, new VirtualChoice(choice.action.DilemmaChoicesList(), choice.node.node.allChilds));
                nextNode.SetChosenAction(choice.action);
                narrationCursor.SetCurrentNode(nextNode);
            }
            else
            {
                narrationCursor.SetCurrentNode(choice.node.node);
            }
            StartStep(NextStep());
        }

        public NarrationNodeBase GetEntryNode(bool continue_narration = false)
        {
            NarrationNodeBase entry;
            if (avatar.Emprisoned())
            {
                return Info.narrations[SPECIAL.SpecialNarration.get_out].GetEntryNode();
            }
            if (avatar.corrupted && currentCell.type == SPECIAL.cureLocation)
            {
                return Info.narrations[SPECIAL.SpecialNarration.cure].GetEntryNode();
            }
            Assert.IsNotNull(currentCell);
            Action will = Service.microworld.TargetWill();
            if (continue_narration)
            {
                will = null;
            }
            entry = Info.narrations[currentCell.type].GetEntryNode(continue_narration);
            if (will != null)
            {
                if (!entry.NarrationPathToAction(true, will, (a1, a2) => a1.WillName() == a2.WillName()))
                {
                    RemoveAnchor(currentPosition);
                    will = null;
                }
            }
            if (will != null && IsHardAnchors(will) && !(will as IAction).IsSpecial(SPECIAL.Tag.repay))
            {
                return new VirtualEvent(
                    $"approach {currentCell.at_the_location} to fullfill your goal to {will.WillName()}",
                    new List<NarrationNodeBase>() {
                        new VirtualActionNode("advance towards my destination and pursue my goal", null, entry.allChilds, new List<Skill>() {SPECIAL.DedicationSkill()}),
                        new VirtualActionNode("give up on my goal and move on", will.WillName(), null, will.failure_location_replacement)
                    }
                );
            }
            return entry;
        }

        public void UpdateAvatar()
        {
            Assert.IsNotNull(currentAction);
            avatar.previousSkills = new Dictionary<string, int>(avatar.skills);
            float duration = currentAction.Duration() * 12f;
            if (duration > 0)
            {
                avatar.memory.UpdateRecall(duration);
                avatar.ageInMonths += duration;
                if (avatar.imbuement.remainingMonths > 0)
                {
                    avatar.imbuement.remainingMonths -= duration;
                    if (avatar.imbuement.remainingMonths <= 0)
                    {
                        avatar.imbuement.remainingMonths = 0;
                    }
                }
            }
            foreach (string itemType in Service.microworldUI.choicesUI.LastUsedItems())
            {
                Item item = Info.items[itemType];
                if (item.consumable)
                {
                    avatar.RemoveFromInventory(itemType);
                }
            }
            if (lastSkillCheckSuccess)
            {
                avatar.Succeed(currentAction);
            }
            else
            {
                avatar.Fail(currentAction);
            }
        }

        public void GoToPrison()
        {
            if (locations.Values.Contains("prison") == false)
            {
                return;
            }
            avatar.state = (int)Avatar.State.EMPRISONED;
            currentPosition = SeachClosestPrison();
            FocusOn(currentPosition);
            Service.microworldUI.iconRawImage.texture = CellIcon(currentPosition);
        }

        public List<string> CurrentDamages()
        {
            HashSet<string> result = new HashSet<string>();
            if (currentAccident != null && currentAccident.damages != null && !currentAction.AccidentPassed())
            {
                result.UnionWith(currentAccident.damages.ToList());
            }
            if (currentAction != null && currentAction.damages != null)
            {
                result.UnionWith(currentAction.damages.ToList());
            }
            return result.ToList();
        }


        public Biome BiomeAt(int idx)
        {
            return Info.biomes[biomeCells[idx]];
        }
        public IEnumerable<Biome> TravelBiomes()
        {
            int i = -1;
            foreach (int n in currentPath.nodes)
            {
                i++;
                if (i == 0) continue;
                yield return BiomeAt(n);
            }
        }
        public HashSet<Biome> UniqueTravelBiomes()
        {
            return new HashSet<Biome>(TravelBiomes());
        }
        public int TravelDuration()
        {
            float d = 0;
            foreach (Biome biome in TravelBiomes())
            {
                d += TravelDuration(biome);
            }
            return Mathf.RoundToInt(d);
        }

        int counter = 0;
        public void Restart()
        {
            avatar = new Avatar();
            message = new Message();
            willAnchors = new Dictionary<int, Action>();
            requirementAnchors = new Dictionary<int, Action>();
            currentAction = null;
            lastSkillCheckSuccessUseGreenNumber = false;
            lastSkillCheckSuccess = true;
            currentAccident = null;
            currentPosition = 0;
            focusPosition = 0;
            targetPosition = -1;
            walk = null;
            _paths = null;
            modifiedCells = new HashSet<int>();
            cells = new Utils.HookableDictionary<int, string>();
            cells.OnValueChanged += (key, value) => {modifiedCells.Add(key);};
            biomeCells = new Dictionary<int, string>();
            biomes = new Dictionary<string, List<int>>();
            locations = new Dictionary<int, string>();

            world.Hide();
            if (runRDB.in_progress.Value == 0)
            {
                currentStep = Step.CHARACTER_CREATION;
            }
            else
            {
                currentStep = Step.LOADING;
            }
            if (Service.microworldNarrator.ready == true)
            {
                StartStep(currentStep);
            }
            GenerateCells();
            int spawn = FindSpawn();
            MoveTo(spawn);
            FocusOn(spawn);
            counter = 0;
            if (runRDB.in_progress.Value != 0)
            {
                LoadRun();
            }
        }

        void LoadRun()
        {
            Debug.Log("DBG LoadRun");
            avatar = new Avatar();
            avatar.state = (int)runRDB.state.Value;
            avatar.corrupted = runRDB.corrupted.Value != 0;

            avatar.imbuement.remainingMonths = (float)runRDB.imbuementRemainingMonth.Value;
            var addict = runRDB.addiction.Value;
            if (addict > 0)
            {
                addict -= 1;
                avatar.imbuement.addiction = SPECIAL.SortedAddictions()[(int)addict];
            }
            else
            {
                avatar.imbuement.addiction = null;
            }
            var imbItem = runRDB.imbuement.Value;
            if (imbItem > 0)
            {
                imbItem -= 1;
                avatar.imbuement.item = Info.items[Info.GetItem(imbItem)];
            }
            else
            {
                avatar.imbuement.item = null;
            }

            avatar.ClearInventory();
            avatar.skills = new Dictionary<string, int>();
            avatar.companions = new List<Companion>();
            avatar.constitution = runRDB.constitution.Value;
            avatar.dexterity = runRDB.dexterity.Value;
            avatar.intelligence = runRDB.intelligence.Value;
            avatar.imagination = runRDB.imagination.Value;
            avatar.charisma = runRDB.charisma.Value;
            avatar.willpower = runRDB.willpower.Value;
            avatar.ageInMonths = runRDB.months.Value;
            avatar.health = runRDB.vitality.Value;
            avatar.greenNumber = runRDB.greenNumber.Value;
            avatar.gold = runRDB.gold.Value;
            for (uint i = 0; i < runRDB.ideas.Length(); i++)
            {
                int ideaIdx = runRDB.ideas[i];
                if (ideaIdx == 0) continue;
                ideaIdx -= 1;
                avatar.ideas.Add(Info.GetIdea(ideaIdx));
            }
            for (uint i = 0; i < runRDB.giveUp.Length(); i++)
            {
                int idx = runRDB.giveUp[i];
                if (idx == 0) continue;
                idx -= 1;
                avatar.giveUpRecord.Add((short)idx);
            }
            for (uint i = 0; i < runRDB.done.Length(); i++)
            {
                int idx = runRDB.done[i];
                if (idx == 0) continue;
                idx -= 1;
                avatar.doneActions.Add((short)idx);
            }
            for (uint i = 0; i < runRDB.skills.Length(); i++)
            {
                SkillRDB skill = runRDB.skills[i];
                if (skill.active.Value == 0) continue;
                float recall = skill.recall.Value;
                if (recall >= 1f)
                {
                    avatar.skills.Add(Info.GetSkill(skill.index.Value), skill.level.Value);
                }
                else
                {
                    avatar.memory.Add(Info.skills[Info.GetSkill(skill.index.Value)], skill.level.Value, recall);
                }
            }
            for (int i = 0; i < 5; i++)
            {
                CompanionRDB companion = companionsRDB[i];
                if (companion.active.Value == 0) continue;
                Companion c = new Companion(
                    Info.GetJob(companion.job.Value),
                    Info.GetCompanionName(companion.name.Value),
                    Info.GetSex(companion.sex.Value)
                );
                c.affinity = companion.affinity.Value;
                c.uuid = companion.uuid.Value;
                c.skills = new Dictionary<string, int>();
                for (uint j = 0; j < companion.skills.Length(); j++)
                {
                    SkillRDB skill = companion.skills[j];
                    if (skill.active.Value == 0) continue;
                    c.skills.Add(Info.GetSkill(skill.index.Value), skill.level.Value);
                }
                PortraitBank(c).Register((uint)c.uuid, companion.portrait.Value);
                avatar.companions.Add(c);
            }
            for (uint i = 0; i < runRDB.inventory.Length(); i++)
            {
                int itemIdx = runRDB.inventory[i];
                if (itemIdx <= 0) continue;
                itemIdx -= 1;
                avatar.AddToInventory(Info.GetItem(itemIdx));
            }
            avatar.vesselCursor = runRDB.vessel.Value;
            avatar.mountCursor = runRDB.vessel.Value;
            for (int i = 0; i < 2; i++)
            {
                avatar.wearingCursors[i] = runRDB.wearing[(uint)i];
            }
            for (int i = 0; i < 5; i++)
            {
                avatar.inhandCursors[i] = runRDB.inhand[(uint)i];
            }
            biomes = new Dictionary<string, List<int>>();
            locations = new Dictionary<int, string>();
            Debug.Log("DBG cells");
            List<int> cellsKeys = cells.Keys.ToList();
            for (int _i = 0; _i < cells.Count; _i++)
            {
                int i = cellsKeys[_i];
                cells[i] = Info.GetCell(worldRDB.cells[(uint)i].cell.Value);
                biomeCells[i] = Info.GetBiome(worldRDB.cells[(uint)i].biome.Value);
                if (worldRDB.cells[(uint)i].location.Value >= 0)
                {
                    locations[i] = Info.GetLocation(worldRDB.cells[(uint)i].location.Value);
                }
                if (biomes.ContainsKey(biomeCells[i]) == false)
                {
                    biomes.Add(biomeCells[i], new List<int>());
                }
                biomes[biomeCells[i]].Add(i);
                if (worldRDB.cells[(uint)i].has_icon.Value != 0)
                {
                    Debug.Log($"DBG load icon {(uint)i}/{worldRDB.cells[(uint)i].icon.Value}");
                    Info.landscapeIcons[Info.cells[cells[i]].icon].Register((uint)i, worldRDB.cells[(uint)i].icon.Value);
                }
            }
            MoveTo(runRDB.position.Value);
            FocusOn(runRDB.position.Value);
            Debug.Log("DBG end");
        }
        void SaveRun()
        {
            Debug.Log("DBG SaveRun");
            Debug.Log("DBG Zero");
            ResetRDB(false);
            Debug.Log("DBG avatar");
            runRDB.in_progress.Value = 1;
            runRDB.state.Value = (byte)avatar.state;
            runRDB.corrupted.Value = avatar.corrupted ? (byte)1 : (byte)0;

            runRDB.imbuementRemainingMonth.Value = avatar.imbuement.remainingMonths;
            if (avatar.imbuement.addiction == null)
            {
                runRDB.addiction.Value = 0;
            }
            else
            {
                runRDB.addiction.Value = (byte)(SPECIAL.SortedAddictions().IndexOf(avatar.imbuement.addiction) + 1);
            }
            if (avatar.imbuement.item == null)
            {
                runRDB.imbuement.Value = 0;
            }
            else
            {
                runRDB.imbuement.Value = (ushort)(Info.GetItemIdx(avatar.imbuement.item.type) + 1);
            }

            runRDB.constitution.Value = (byte)avatar.constitution;
            runRDB.dexterity.Value = (byte)avatar.dexterity;
            runRDB.intelligence.Value = (byte)avatar.intelligence;
            runRDB.imagination.Value = (byte)avatar.imagination;
            runRDB.charisma.Value = (byte)avatar.charisma;
            runRDB.willpower.Value = (byte)avatar.willpower;
            runRDB.months.Value = (ushort)avatar.ageInMonths;
            runRDB.vitality.Value = (byte)avatar.health;
            runRDB.greenNumber.Value = (byte)avatar.greenNumber;
            runRDB.gold.Value = (ushort)avatar.gold;
            var giveUpList = avatar.giveUpRecord.ToList();
            for (uint i = 0; i < runRDB.giveUp.Length(); i++)
            {
                if (i >= giveUpList.Count) break;
                runRDB.giveUp[i] = (short)(giveUpList[(int)i] + 1);
            }
            var doneList = avatar.doneActions.ToList();
            for (uint i = 0; i < runRDB.done.Length(); i++)
            {
                if (i >= doneList.Count) break;
                runRDB.done[i] = (short)(doneList[(int)i] + 1);
            }
            for (uint i = 0; i < runRDB.ideas.Length(); i++)
            {
                if (i >= avatar.ideas.Count) break;
                int ideaIdx = Info.GetIdeaIdx(avatar.ideas[(int)i]);
                ideaIdx += 1;
                runRDB.ideas[i] = (ushort)ideaIdx;
            }
            var memory = avatar.memory.skills.ToList();
            memory.Reverse();
            for (uint i = 0; i < runRDB.skills.Length(); i++)
            {
                if (i < avatar.skills.Count)
                {
                    string skill = avatar.skills.Keys.ToList()[(int)i];
                    runRDB.skills[i].index.Value = (ushort)Info.GetSkillIdx(skill);
                    runRDB.skills[i].level.Value = (ushort)avatar.skills[skill];
                    runRDB.skills[i].recall.Value = 1f;
                    runRDB.skills[i].active.Value = 1;
                }
                else
                {
                    int j = (int)i - (int)avatar.skills.Count;
                    if (j < memory.Count)
                    {
                        string skill = memory[j];
                        runRDB.skills[i].index.Value = (ushort)Info.GetSkillIdx(skill);
                        runRDB.skills[i].level.Value = (ushort)avatar.memory.level[skill];
                        runRDB.skills[i].recall.Value = avatar.memory.recallThreshold[skill];
                        runRDB.skills[i].active.Value = 1;
                    }

                }
            }
            for (int i = 0; i < 5; i++)
            {
                if (i >= avatar.companions.Count) continue;
                Companion companion = avatar.companions[(int)i];
                companionsRDB[i].active.Value = 1;
                companionsRDB[i].affinity.Value = (short)companion.affinity;
                companionsRDB[i].uuid.Value = companion.uuid;
                companionsRDB[i].portrait.Value = (ushort)PortraitBank(companion).GetImgId((uint)companion.uuid);
                companionsRDB[i].name.Value = (ushort)Info.GetCompanionNameIdx(companion.name);
                companionsRDB[i].job.Value = (ushort)Info.GetJobIdx(companion.job);
                companionsRDB[i].sex.Value = (byte)Info.GetSexIdx(companion.sex);
                for (uint j = 0; j < companion.skills.Count; j++)
                {
                    string skill = companion.skills.Keys.ToList()[(int)j];
                    companionsRDB[i].skills[j].index.Value = (ushort)Info.GetSkillIdx(skill);
                    companionsRDB[i].skills[j].level.Value = (ushort)companion.skills[skill];
                    companionsRDB[i].skills[j].active.Value = 1;
                }
            }
            var inventory = avatar.GetInventory().ToList();
            for (uint i = 0; i < runRDB.inventory.Length(); i++)
            {
                if (i >= inventory.Count()) continue;
                runRDB.inventory[i] = (ushort)(Info.GetItemIdx(inventory[(int)i]) + 1);
            }
            runRDB.vessel.Value = avatar.vesselCursor;
            runRDB.mount.Value = avatar.mountCursor;
            for (int i = 0; i < 2; i++)
            {
                runRDB.wearing[(uint)i] = avatar.wearingCursors[i];
            }
            for (int i = 0; i < 5; i++)
            {
                runRDB.inhand[(uint)i] = avatar.inhandCursors[i];
            }
            runRDB.position.Value = currentPosition;
            foreach (int i in modifiedCells)
            {
                Profiler.BeginSample("rdb cell");
                CellRDB c = worldRDB.cells[(uint)i];
                Profiler.EndSample();

                Profiler.BeginSample("get cell");
                Cell cell = Info.cells[cells[(int)i]];
                Profiler.EndSample();

                Profiler.BeginSample("get cell idx");
                c.cell.Value = (byte)Info.GetCellIdx(cell.type);
                Profiler.EndSample();
                Profiler.BeginSample("get biome idx");
                c.biome.Value = (byte)Info.GetBiomeIdx(biomeCells[i]);
                Profiler.EndSample();

                Profiler.BeginSample("set default");
                c.has_icon.Value = 0;
                c.location.Value = -1;
                Profiler.EndSample();
            }
            Profiler.BeginSample("check location");
            foreach (var pair in locations)
            {
                int i = pair.Key;
                CellRDB c = worldRDB.cells[(uint)i];
                c.location.Value = (short)Info.GetLocationIdx(pair.Value);
            }
            Profiler.EndSample();
            Profiler.BeginSample("check landscape");
            foreach (var bankpair in Info.landscapeIcons)
            {
                var bank = bankpair.Value;
                var bankName = bankpair.Key;
                foreach (var pair in bank.assigned)
                {
                    CellRDB c = worldRDB.cells[pair.Key];
                    Cell cell = Info.cells[cells[(int)pair.Key]];
                    if (cell.icon != bankName) continue;
                    c.icon.Value = (ushort)bank.GetImgId(pair.Key);
                    c.has_icon.Value = 1;
                }
            }
            Profiler.EndSample();
            modifiedCells.Clear();
        }

        List<int> rndCharIdxs;
        public bool multipleInstanceError = false;
        void Start()
        {
            Info.Load();
            Debug.Log("override default rendering");
            Application.targetFrameRate = Config.framerate;
            OnDemandRendering.renderFrameInterval = Config.renderFrameInterval;
            multipleInstanceError = false;
            Service.music.Play("intro");
            try
            {
                try
                {
                    Service.rdb.Open();
                    runRDB = Service.rdb.Get<RunRDB>(0);
                    worldRDB = Service.rdb.Get<WorldRDB>(0);
                    for (int i = 0; i < 5; i++)
                    {
                        companionsRDB.Add(Service.rdb.Get<CompanionRDB>(i));
                    }
                }
                catch (FileNotFoundException ex)
                {
                    Service.rdb.Reset();
                    runRDB = Service.rdb.New<RunRDB>();
                    worldRDB = Service.rdb.New<WorldRDB>();
                    for (int i = 0; i < 5; i++)
                    {
                        companionsRDB.Add(Service.rdb.New<CompanionRDB>());
                    }
                }

            }
            catch (IOException ex)
            {
                avatar = new Avatar();
                multipleInstanceError = true;
                return;
            }
            World.WorldObject.QuickMake(new World.ClockState());
            world = World.WorldObject.QuickMake<AsciiWorld>() as AsciiWorld;
            graph = World.WorldObject.QuickMake<WorldGraph>() as WorldGraph;
            world.Hide();

            int vi = 0;
            foreach (CharPixel cp in world.render.sphere.charPixels)
            {
                randomNumbers.Add(Utils.RandomInt());
                int i = vi;
                cp.triggerEnter = () => MouseEnter(i);
                cp.triggerExit = () => MouseExit(i);
                // cp.triggerDown = () => MouseDown(i, true);
                // cp.triggerDownRight = () => MouseDown(i, false);
                vi++;
            }
            Restart();
            rndCharIdxs = Utils.ShuffleRange(nchars).ToList();
            Service.microworldUI.postprocessors.Add(GreenModeUI);
        }

        int FindSpawn()
        {
            foreach (int idx in Utils.ShuffleRange(cells.Count))
            {
                if (cells.ContainsKey(idx))
                {
                    if (cells[idx] == SPECIAL.spawnBiome)
                    {
                        return idx;
                    }
                }
            }
            return 0;
        }

        public IEnumerable<int> NeighborCell(int cell)
        {
            return graph.LinkedTo(cell);
        }
        public int NeighborToward(int from, int to)
        {
            float bestDist = Mathf.Infinity;
            int best = to;
            foreach (int n in graph.LinkedTo(from))
            {
                float dist = Geometry.Distance(mesh.Positions[n], mesh.Positions[to]);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = n;
                }
            }
            return best;
        }

        public int nchars => world.render.sphere.charPixels.Count;
        CharPixel GetChar(int vi)
        {
            return world.render.sphere.charPixels[vi];
        }
        bool IsOnFocus(int vi)
        {
            return Distance(vi, focusPosition) < camRadius;
        }
        bool IsClickable(int vi)
        {
            return Distance(vi, focusPosition) < mouseRadius;
        }
        bool IsNear(int vi)
        {
            return Distance(vi, focusPosition) < nearRadius;
        }

        void MouseEnter(int vi)
        {
            if (currentStep != Step.CHOOSE_DESTINATION) return;
            if (vi == currentPosition && !currentCell.can_reclick)
            {
                targetPosition = -1;
                return;
            }
            if (!IsClickable(vi))
            {
                targetPosition = -1;
                return;
            }
            targetPosition = vi;
            if (targetPosition < 0) return;
            ResetPaths();
        }
        void MouseExit(int vi)
        {
            if (currentStep != Step.CHOOSE_DESTINATION) return;
            if (!IsClickable(vi))
            {
                return;
            }
            targetPosition = -1;
        }

        public bool Ready()
        {
            return Service.microworldNarrator.ready && currentStep != Step.CHARACTER_CREATION;
        }

        public void SkipMessage()
        {
            StartStep(NextStep());
        }
        public void SkipPopup()
        {
            StartStep(NextStep());
        }

        int lastAccidentCell = -1;
        int lastChattingCell = -1;
        public Companion chattingWith = null;
        public Item soakUpItem = null;
        bool AccidentHappens()
        {
            if (currentPosition == lastAccidentCell) return false;
            if (currentPosition == lastChattingCell) return false;
            float risk = RiskAt(currentPosition);
            List<Accident> possibleAccidents = BiomeAt(currentPosition).PossibleAccidents().ToList();
            if (possibleAccidents.Count == 0) return false;
            Accident accident = possibleAccidents.Random();
            if (Utils.RandomFloat() < risk)
            {
                Service.microworldNarrator.Reset();
                narrationCursor.SetCurrentNode(
                    new VirtualEvent(
                        $"about to {accident.about_to}",
                        new VirtualChoice(accident.Reactions().Cast<IAction>().ToList())
                    )
                );
                currentAccident = accident;
                StartStep(Step.EVENT_NARRATION);
                lastAccidentCell = currentPosition;
                if (Service.microworldUI.iconRawImage != null)
                {
                    Service.microworldUI.iconRawImage.texture = CellIcon(currentPosition, true);
                }
                return true;

            }
            return false;
        }
        bool ChattingHappens()
        {
            if (currentPosition == lastAccidentCell) return false;
            if (currentPosition == lastChattingCell) return false;
            if (avatar.companions.Count <= 0) return false;
            if (Utils.RandomFloat() < 0.03)
            {
                chattingWith = avatar.companions.Random();
                if (Service.microworldUI.iconRawImage != null) Service.microworldUI.iconRawImage.texture = Portrait(chattingWith);
                Service.microworldNarrator.Reset();
                narrationCursor.SetCurrentNode(new TravelChatEvent(chattingWith));
                StartStep(Step.EVENT_NARRATION);
                lastChattingCell = currentPosition;
                return true;
            }
            return false;
        }
        public IEnumerable<string> ConsumptionLastEffects()
        {
            if (consumptionCureCorruption) yield return "body humors corruption has been cured";
            if (consumptionCureAddiction) yield return "addiction has been cured";
            if (greenMode) yield return "green number vision granted";
        }

        public bool consumptionAddictionResistanceCheck;
        public bool consumptionCorruptionResistanceCheck;
        public bool consumptionCureCorruption;
        public bool consumptionCureAddiction;
        public void SoakUp(Item item)
        {
            resetOpportunities = false;
            consumptionAddictionResistanceCheck = Utils.RandomRange(0, 100) < (avatar.addictionResistance * 100f);
            consumptionCorruptionResistanceCheck = Utils.RandomRange(0, 100) < (avatar.humorCorruptionResistance * 100f);
            if (item.consumable)
            {
                avatar.RemoveFromInventory(item.type);
            }
            if (item.addiction == null || item.addiction.Length == 0)
            {
                consumptionAddictionResistanceCheck = true;
            }
            else if (!consumptionAddictionResistanceCheck)
            {
                avatar.imbuement.addiction = item.addiction;
            }
            if (item.can_corrupt == false)
            {
                consumptionCorruptionResistanceCheck = true;
            }
            else if (!consumptionCorruptionResistanceCheck)
            {
                avatar.corrupted = true;
            }
            if (consumptionCorruptionResistanceCheck)
            {
                avatar.imbuement.item = item;
                avatar.imbuement.remainingMonths = Math.Max(0, (int)item.ImbuementDuration());
            }
            else
            {
                avatar.imbuement.item = null;
                avatar.imbuement.remainingMonths = 0;
            }
            consumptionCureCorruption = false;
            consumptionCureAddiction = false;
            if (avatar.corrupted && item.can_cure)
            {
                consumptionCureCorruption = true;
                avatar.corrupted = false;
            }
            if (avatar.addicted && item.detox)
            {
                consumptionCureAddiction = true;
                avatar.imbuement.addiction = null;
            }
            soakUpItem = item;
            Service.microworldNarrator.Reset();
            narrationCursor.SetCurrentNode(new ConsumptionEvent(item));
            if (consumptionAddictionResistanceCheck && consumptionCorruptionResistanceCheck)
            {
                Service.audio.Play("success");
            }
            else
            {
                Service.audio.Play("fail");
            }
            StartStep(Step.EVENT_NARRATION);
        }


        public static string FormatDuration(int nmonths)
        {
            int years = nmonths / 12;
            int months = nmonths % 12;
            string yearStr = $"{years} years";

            if (years == 1)
            {
                yearStr = $"{years} year";
            }
            string monthStr = $"{months} months";
            if (months == 1)
            {
                monthStr = $"{months} month";
            }
            string timeStr = $"{yearStr} and {monthStr}";
            if (years == 0)
            {
                timeStr = $"{monthStr}";
            }
            else if (months == 0)
            {
                timeStr = $"{yearStr}";
            }
            return timeStr;
        }
        public class ConsumptionMessage : EventInfo
        {
            public string what_just_happened => $"soak up {Service.microworld.soakUpItem.type}";
            public string[] new_skill_buffs => (
                (!Service.microworld.consumptionCorruptionResistanceCheck) ?
                null :
                Service.microworld.soakUpItem.buffs.ToArray()
            );
            public string[] other_effects => (
                Service.microworld.ConsumptionLastEffects().Count() > 0 ?
                Service.microworld.ConsumptionLastEffects().ToArray() :
                null
            );
            public string new_addiction => (
                Service.microworld.consumptionAddictionResistanceCheck ?
                null :
                Service.microworld.soakUpItem.addiction
            );
            public string new_affliction => (
                Service.microworld.consumptionCorruptionResistanceCheck ?
                null :
                "body humors corrupted by the substance"
            );
        }

        public class TravelChattingMessage : EventInfo
        {
            public string when => $"while traveling with my companions";
            public string where => $"{Service.microworld.BiomeAt(Service.microworld.currentPosition).travelling_in_a_biome}";
            public string what_just_happened => Service.microworld.narrationCursor.currentNode.What();
            public string location_mood => Service.microworld.CurrentCellMood();
            public string next_action_to_do => "choose which topic to discuss";
        }
        public class AccidentMessage : EventInfo
        {
            public string when => $"while traveling";
            public string where => $"{Service.microworld.BiomeAt(Service.microworld.currentPosition).travelling_in_a_biome}";
            public string what_just_happened => $"about to {Service.microworld.currentAccident.about_to}";
            public string outcome = "pending";
            public string location_mood => Service.microworld.CurrentCellMood();
            public string[] next_choices_to_take_from_now_on => Service.microworld.narrationCursor.nextChoices.Select(c => c.action.Text4LLM()).ToArray();
        }
        public class EventMessage : EventInfo
        {
            public string where => Service.microworld.currentCell.at_the_location;
            public string location_mood => Service.microworld.CurrentCellMood();
            public string what_just_happened => Service.microworld.narrationCursor.currentNode.What();
            public string what_is_happening => (Service.microworld.narrationCursor.currentNode.What() != Service.microworld.narrationCursor.Consequence() ? Service.microworld.narrationCursor.Consequence() : null);
            public string context => Service.microworld.narrationCursor.Context();
        }
        public class ChoiceMessage : EventInfo
        {
            public string where => Service.microworld.currentCell.at_the_location;
            public string what_just_happened => Service.microworld.narrationCursor.currentNode.What();
            public string what_is_happening => (Service.microworld.narrationCursor.currentNode.What() != Service.microworld.narrationCursor.Consequence() ? Service.microworld.narrationCursor.Consequence() : null);
            public string location_mood => Service.microworld.CurrentCellMood();
            public string context => Service.microworld.narrationCursor.Context();
            public string[] next_choices_to_take_from_now_on
            {
                get
                {
                    if (Service.microworld.narrationCursor.nextChoices == null || Service.microworld.narrationCursor.nextChoices.Count <= 1)
                    {
                        return null;
                    }
                    return Service.microworld.narrationCursor.nextChoices.Select(c => c.action.Text4LLM()).ToArray();
                }
            }
            public string next_action_to_do
            {
                get
                {
                    if (Service.microworld.narrationCursor.nextChoices == null || Service.microworld.narrationCursor.nextChoices.Count != 1)
                    {
                        return null;
                    }
                    return Service.microworld.narrationCursor.nextChoices[0].action.Text4LLM();
                }
            }
        }

        public class TravelTowardMessage : EventInfo
        {
            public string when => $"{Service.microworld.avatar.Years()} years old";
            public string what_just_happened => $"start a travel of {FormatDuration(Service.microworld.TravelDuration())} {Service.microworld.targetCell.toward_the_location}";
            public string green_number_vision_content => (
                (Service.microworld.TargetWill() != null && !Service.microworld.IsPlanAction(Service.microworld.TargetWill())) ?
                Service.microworld.TargetWill().WillName() :
                null
            );
            public string with_what_goal => (
                (Service.microworld.avatar.corrupted && Service.microworld.cells[Service.microworld.targetPosition] == SPECIAL.cureLocation) ?
                "to receive a cure" :
                (
                    (Service.microworld.TargetWill() != null && !Service.microworld.IsPlanAction(Service.microworld.TargetWill())) ?
                    "to follow a green number vision" :
                    (Service.microworld.TargetWill() == null ? null : $"to {Service.microworld.TargetWill().WillName()}")
                )
            );
        }
        public class FirstMessage : EventInfo
        {
            public string when => $"{(int)(Service.microworld.avatar.ageInMonths / 12f)} years old";
            public string what_just_happened => $"leave home";
            public string why => $"go on an adventure";
        }
        public class ActionMessage : EventInfo
        {
            public string where => $"{Service.microworld.currentCell.at_the_location}";
            public string context => Service.microworld.narrationCursor.Context();
            public string what_just_happened => "chose to " + Service.microworld.narrationCursor.currentNode.What();
            public string what_is_happening => (Service.microworld.narrationCursor.currentNode.What() != Service.microworld.narrationCursor.Consequence() ? Service.microworld.narrationCursor.Consequence() : null);
            public string outcome = "unknown";
            public string companion_answer = null;
            public string new_loction_added_to_map = null;
            public List<string> skills_used = null;
            public List<string> lacking_skills = null;
            public List<string> successfully_helped_by_companions = null;
            public List<string> unsuccessfully_helped_by_companions = null;
            public List<string> items_used = null;
            public List<string> rewards = null;
            public List<string> penalty = null;
            public List<string> next_choices_to_take_from_now_on = null;
            public string next_action_to_do = null;
            public string final_goal = (Service.microworld.IsAtTarget() && Service.microworld.TargetWill() != null ? Service.microworld.TargetWill().WillName() : null);
            public ActionMessage()
            {
                List<string> used = null;
                List<string> lacks = null;
                if (Service.microworld.lastChosenSkills != null)
                {
                    used = Service.microworld.lastChosenSkills.Keys.Select(s => s.type).ToList();
                    if (Service.microworld.currentAction.Skills() != null)
                    {
                        lacks = Service.microworld.currentAction.Skills().Select(s => s.type).Where(s => used.Contains(s) == false).ToList();
                    }
                }
                if (Service.microworld.currentAction.Difficulty() >= 0)
                {
                    if (Service.microworld.lastSkillCheckSuccessUseGreenNumber)
                    {
                        skills_used = new List<string>() { "luck" };
                    }
                    else if (Service.microworld.lastChosenSkills != null && Service.microworld.lastChosenSkills.Count > 0)
                    {
                        skills_used = used.Take(3).ToList();
                    }
                    if (Service.microworldUI.choicesUI.LastUsedItems().Count() > 0)
                    {
                        items_used = Service.microworldUI.choicesUI.LastUsedItems().Take(2).ToList();
                    }
                    if (Service.microworldUI.choicesUI.LastUsedCompanions().Count() > 0)
                    {
                        if (Service.microworld.lastSkillCheckSuccess == false)
                        {
                            unsuccessfully_helped_by_companions = Service.microworldUI.choicesUI.LastUsedCompanions().Take(2).ToList();
                        }
                        else
                        {
                            successfully_helped_by_companions = Service.microworldUI.choicesUI.LastUsedCompanions().Take(2).ToList();
                        }
                    }
                }
                if (Service.microworld.currentAction.Difficulty() >= 0 && Service.microworld.lastSkillCheckSuccess == false)
                {
                    if (lacks != null && lacks.Count > 0)
                    {
                        lacking_skills = lacks;
                    }
                    if (Service.microworld.chattingWith != null)
                    {
                        penalty = new List<string>
                            {
                                "loose affinity with my traveling companion"
                            };
                    }
                    else if (Service.microworld.currentAction.damages != null && Service.microworld.currentAction.damages.Count() > 0)
                    {
                        penalty = Service.microworld.currentAction.damages.Select(dmg => SPECIAL.damagesMsg[dmg]).ToList();
                        if (Service.microworld.avatar.health <= 0)
                        {
                            penalty.Add("death");
                        }
                    }
                    else if (!Service.microworld.IsAtTarget() && Service.microworld.currentAccident.damages != null && Service.microworld.currentAccident.damages.Count() > 0 && !Service.microworld.currentAction.AccidentPassed())
                    {
                        penalty = Service.microworld.currentAccident.damages.Select(dmg => SPECIAL.damagesMsg[dmg]).ToList();
                        if (Service.microworld.avatar.health <= 0)
                        {
                            penalty.Add("death");
                        }
                    }
                    else
                    {
                        penalty = new List<string>() { "scrapped" };
                    }
                    if (Service.microworld.avatar.health > 0 && Service.microworld.IsAtTarget())
                    {
                        if (Service.microworld.avatar.Emprisoned())
                        {
                            next_action_to_do = "still locked up in prison";
                        }
                        else
                        {
                            next_action_to_do = "need to leave the place";
                        }
                    }
                    if (Service.microworld.avatar.health > 0 && !Service.microworld.IsAtTarget())
                    {
                        if (Service.microworld.chattingWith != null)
                        {
                            next_action_to_do = "continue my travel in silence";
                        }
                        else
                        {
                            next_action_to_do = "resume my journey while weakened";
                        }
                    }
                }
                if (Service.microworld.lastSkillCheckSuccess == true)
                {
                    if (Service.microworld.narrationCursor.last == true)
                    {
                        if (Service.microworld.chattingWith != null)
                        {
                            next_action_to_do = "continue my travel in camaraderie";
                        }
                        else if (Service.microworld.ContinueNewLocation())
                        {
                            next_action_to_do = (Service.microworld.currentCell as Location).go_in;
                        }
                        else
                        {
                            next_action_to_do = "time to continue my travel";
                        }
                    }
                    var action = Service.microworld.currentAction;
                    if (Service.microworld.ActionNewLocation() != null && action.new_location_type != SPECIAL.NewLocationTypes.RESET)
                    {
                        new_loction_added_to_map = Service.microworld.ActionNewLocation().type;
                    }
                    rewards = new List<string>();
                    if (action.companion != null && action.companion.Length > 0 && Service.microworld.avatar.lastCompanion != null)
                    {
                        Companion companion = Service.microworld.avatar.lastCompanion;
                        string txt = $"{companion.Name()} is now following me";
                        rewards.Add(txt);

                    }
                    foreach (string idea in action.ideas)
                    {
                        string txt = $"inspired to {idea}";
                        rewards.Add(txt);
                    }
                    foreach (string skill in Service.microworld.avatar.skills.Keys)
                    {
                        int lvl = Service.microworld.avatar.skills[skill];
                        if (Service.microworld.avatar.previousSkills.ContainsKey(skill) == false)
                        {
                            string txt = $"learned skill: {skill}";
                            rewards.Add(txt);

                        }
                        else if (Service.microworld.avatar.previousSkills[skill] != Service.microworld.avatar.skills[skill])
                        {
                            string txt = $"improve skill: {skill}";
                            rewards.Add(txt);
                        }
                    }
                    if (Service.microworld.chattingWith != null)
                    {
                        if (Service.microworld.lastSkillCheckSuccess)
                        {
                            rewards.Add("gain affinity with my traveling companion");
                            companion_answer = (Service.microworld.narrationCursor.currentNode as QuestionNode).answer;
                        }
                    }
                    foreach (Item item in action.RandomItems())
                    {
                        string txt = $"gain item: {item.Name()}";
                        rewards.Add(txt);
                    }
                    if (action.Gold() > 0)
                    {
                        string txt = $"won {action.Gold()} gold coins";
                        rewards.Add(txt);
                    }
                    if (action.IsSpecial(SPECIAL.Tag.free))
                    {
                        string txt = $"free from prison";
                        rewards.Add(txt);
                    }
                    if (action.IsSpecial(SPECIAL.Tag.cure))
                    {
                        string txt = $"my humors corruption desease has been cured";
                        rewards.Add(txt);
                    }
                    if (rewards.Count == 0)
                    {
                        rewards = null;
                    }
                }
                if (Service.microworld.narrationCursor.nextChoices != null && Service.microworld.narrationCursor.nextChoices.Count > 0 && Service.microworld.lastSkillCheckSuccess == true)
                {
                    if (Service.microworld.narrationCursor.nextChoices.Count > 1)
                    {
                        next_choices_to_take_from_now_on = Service.microworld.narrationCursor.nextChoices.Select(c => c.action.Text4LLM()).ToList();
                    }
                    else if (Service.microworld.narrationCursor.nextChoices.Count == 1)
                    {
                        next_action_to_do = Service.microworld.narrationCursor.nextChoices[0].action.Text4LLM();
                    }
                    if (final_goal != null)
                    {
                        outcome = "intermediate step toward final goal achieved";
                    }
                    else
                    {
                        outcome = "pending";
                    }
                }
                else
                {
                    if (Service.microworld.chattingWith != null)
                    {
                        if (Service.microworld.lastSkillCheckSuccess)
                        {
                            outcome = "my companion answer";
                        }
                        else
                        {
                            outcome = "my companion does not answer";
                        }
                    }
                    else
                    {
                        if (Service.microworld.currentAction.Difficulty() < 0)
                        {
                            outcome = null;
                        }
                        else
                        {
                            outcome = (Service.microworld.lastSkillCheckSuccess ? "success" : "failure");
                        }
                    }
                }
            }
        }

        float RiskAt(int idx)
        {
            Biome biome = this.BiomeAt(idx);
            float risk = biome.accident_risk;
            foreach (Item item in avatar.UsingItems(biome))
            {
                if (item.accident_reduction >= 0 && item.accident_reduction <= 1f)
                {
                    if (item.biomes != null && item.biomes.Contains(biome.type))
                    {
                        risk *= item.accident_reduction;
                    }
                }

            }
            return risk;
        }


        public float TravelRisk()
        {
            if (currentPath == null || currentPath.nodes.Count <= 1) return 0f;
            float noRisk = 1f;
            foreach (int cellIdx in currentPath.nodes)
            {
                if (cellIdx == currentPath.nodes.Last()) break;
                float cellRisk = RiskAt(cellIdx);
                noRisk *= (1f - cellRisk);
            }
            return 1f - noRisk;
        }

        public int usedGreenNumber;
        public int luckRoleDice;

        public void RoleGreenNumber()
        {
            usedGreenNumber = avatar.greenNumber;
            luckRoleDice = Utils.RandomRange(0, 100);
            avatar.greenNumber = avatar.GreenNumberInitValue();
        }

        public IEnumerable<string> SkillChoices()
        {
            List<Skill> skills = avatar.Skills().ToList();
            return skills.Select(s => s.type);
        }
        public IEnumerable<string> SkillToRemoveLabels()
        {
            List<Skill> skills = avatar.Skills().ToList();
            foreach (var s in skills)
            {
                string moveResidual = "";
                if (avatar.SkillLevel(s) > avatar.startLvl)
                {
                    moveResidual = "*";
                }
                yield return $"{moveResidual}{s.type} ({s.Characteristic()[..3].ToUpper()})";
            }
        }

        public Dictionary<Skill, List<int>> lastChosenSkills = null;
        public void SelectSkills(Dictionary<Skill, List<int>> agg)
        {
            lastSkillCheckSuccess = false;
            lastSkillCheckSuccessUseGreenNumber = false;
            Dictionary<Skill, int> skills = agg.ToDictionary(pair => pair.Key, pair => pair.Value.Sum());
            skillChecker = new SkillChecker(skills, currentAction.Skills().ToList(), currentAction.Difficulty());
            skillChecker.Setup();
            lastChosenSkills = agg;
            StartStep(NextStep());
        }
        public Skill GetSkill(int idx)
        {
            List<Skill> choices = SkillChoices().Select(s => Info.skills[s]).ToList();
            return choices[idx];
        }
        public void RemoveSkillSelection(List<int> selections)
        {
            List<Skill> choices = SkillChoices().Select(s => Info.skills[s]).ToList();
            List<Skill> todel = new List<Skill>();
            foreach (int s in selections)
            {
                todel.Add(GetSkill(s));
            }
            foreach (Skill s in todel)
            {
                avatar.ForgetSkill(s);
            }
            Assert.IsTrue(avatar.skills.Count <= avatar.maxSkills);
            StartStep(NextStep());
        }
        public void RemoveCompanionSelection(List<int> selection)
        {
            List<Companion> toRemove = selection.Select(idx => avatar.companions[idx]).ToList();
            foreach (Companion c in toRemove)
            {
                avatar.companions.Remove(c);
            }
            Assert.IsTrue(avatar.companions.Count <= avatar.maxCompanions);
            StartStep(NextStep());
        }
        public void RemoveIdeaSelection(List<int> selection)
        {
            List<string> toRemove = selection.Select(idx => avatar.ideas[idx]).ToList();
            foreach (string s in toRemove)
            {
                avatar.ideas.Remove(s);
            }
            Assert.IsTrue(avatar.ideas.Count <= avatar.maxIdeas);
            StartStep(NextStep());
        }

        public bool Walking() => walk != null && walk.Count > 0;

        bool update = false;

        Vector3 RandomVectorOffset(RNG rng)
        {
            return new Vector3(
                rng.RandomRange(-1000f, 1000f),
                rng.RandomRange(-1000f, 1000f),
                rng.RandomRange(-1000f, 1000f)
            );
        }
        void AddBiome(int idx, string biome)
        {
            Assert.IsFalse(cells.ContainsKey(idx));
            cells.Add(idx, biome);
            biomeCells.Add(idx, biome);
            if (biomes.ContainsKey(biome) == false)
            {
                biomes.Add(biome, new List<int>());
            }
            Assert.IsFalse(biomes[biome].Contains(idx));
            biomes[biome].Add(idx);
        }

        int FindNewLocationCell(Cell target, string where)
        {
            Assert.IsTrue(SPECIAL.AllNewLocationTypes.Contains(where));
            if (where == SPECIAL.NewLocationTypes.HERE || where == SPECIAL.NewLocationTypes.RESET)
            {
                return currentPosition;
            }
            List<string> biomes;
            if (target is Biome)
            {
                biomes = new List<string>() { target.type };
            }
            else
            {
                biomes = (target as Location).biomes.ToList();
            }
            List<int> hearOK = new List<int>();
            List<int> all = new List<int>();
            List<int> allOK = new List<int>();
            List<int> inRange = new List<int>();
            List<int> inRangeOK = new List<int>();
            List<int> near = new List<int>();
            List<int> nearOK = new List<int>();
            foreach (int idx in cells.Keys)
            {
                Cell cell = Info.cells[cells[idx]];
                string biome = biomeCells[idx];
                bool ok = (biomes.Contains(biome) && (locations.ContainsKey(idx) == false));
                bool isInRange = IsClickable(idx);
                bool isNear = IsNear(idx);
                if (idx == currentPosition)
                {
                    if (ok) hearOK.Add(idx);
                    continue;
                }
                all.Add(idx);
                if (ok)
                {
                    allOK.Add(idx);
                }
                if (isInRange)
                {
                    inRange.Add(idx);
                    if (ok)
                    {
                        inRangeOK.Add(idx);
                    }
                }
                if (isNear)
                {
                    near.Add(idx);
                    if (ok)
                    {
                        nearOK.Add(idx);
                    }
                }
            }
            Assert.IsTrue(all.Count > 0);
            if (where == SPECIAL.NewLocationTypes.NEAR)
            {
                if (nearOK.Count > 0) return nearOK.Random();
                if (hearOK.Count > 0) return hearOK.Random();
                if (near.Count > 0) return near.Random();
                if (inRangeOK.Count > 0) return inRangeOK.Random();
                if (inRange.Count > 0) return inRange.Random();
                if (allOK.Count > 0) return allOK.Random();
                return all.Random();
            }
            if (where == SPECIAL.NewLocationTypes.SOMEWHERE)
            {
                if (inRangeOK.Count > 0) return inRangeOK.Random();
                if (allOK.Count > 0) return allOK.Random();
                return all.Random();
            }
            Assert.IsTrue(false);
            return -1;
        }
        public void OverrideLocation(Cell cell, string where)
        {
            int idx = FindNewLocationCell(cell, where);
            Assert.IsTrue(cells.ContainsKey(idx));
            cells[idx] = cell.type;
            lastNewLocationCell = idx;
            if (cell is Location)
            {
                if (locations.ContainsKey(idx) == false)
                {
                    locations.Add(idx, cell.type);
                }
                else
                {
                    locations[idx] = cell.type;
                }
            }
            else
            {
                if (locations.ContainsKey(idx) == false)
                {
                    locations.Remove(idx);
                }
            }
            RemoveAnchor(idx);
        }

        void AddLocation(int idx, string location)
        {
            Assert.IsTrue(cells.ContainsKey(idx));
            Assert.IsFalse(locations.ContainsKey(idx));
            cells[idx] = location;
            locations.Add(idx, location);
        }

        void GenerateCells()
        {
            int seed = Utils.RandomInt();
            Debug.Log("world seed : " + seed);
            RNG rng = new RNG(seed);
            Vector3 off1 = RandomVectorOffset(rng);
            Vector3 off2 = RandomVectorOffset(rng);
            Vector3 off3 = RandomVectorOffset(rng);

            for (int idx = 0; idx < mesh.Positions.Count; idx++)
            {
                Vector3 p1 = (off1 + mesh.Positions[idx]) / 12f;
                Vector3 p2 = (off2 + mesh.Positions[idx]) / 3f;
                Vector3 p3 = (off3 + mesh.Positions[idx]) / 8f;
                float perlinNoise1 = Perlin.Noise(p1.x, p1.y, p1.z);
                float perlinNoise2 = Perlin.Noise(p2.x, p2.y, p2.z);
                float perlinNoise3 = Perlin.Noise(p3.x, p3.y, p3.z);

                // WATER
                if (perlinNoise1 <= -0.25f)
                {
                    AddBiome(idx, "ocean");
                    continue;
                }
                if (perlinNoise1 <= 0)
                {
                    AddBiome(idx, "sea");
                    continue;
                }

                // MOUTAIN
                if (perlinNoise3 > 0.5f)
                {
                    AddBiome(idx, "peak");
                    continue;
                }
                if (perlinNoise3 > 0.3f)
                {
                    AddBiome(idx, "mountain");
                    continue;
                }

                // CITY
                if (perlinNoise2 < -0.4f)
                {
                    AddBiome(idx, "city");
                    continue;
                }


                // COAST
                if (perlinNoise1 <= 0.065f)
                {
                    AddBiome(idx, "coast");
                    continue;
                }

                // FOREST
                if (perlinNoise2 > 0.25f)
                {
                    AddBiome(idx, "forest");
                    continue;
                }

                // FIELD
                if (perlinNoise2 < -0.15f)
                {
                    AddBiome(idx, "field");
                    continue;
                }
                // PLAIN
                AddBiome(idx, "plain");
            }
            AddRandomLocations(rng);
            AddAtLeastOnePrison(rng);
            Debug.Log("number of cells : " + cells.Count);
        }

        void AddRandomLocations(RNG rng)
        {
            foreach (var pair in biomes)
            {
                Biome biome = Info.biomes[pair.Key];
                List<int> allCells = pair.Value;
                allCells.Shuffle(rng);
                int ntries = (int)((float)allCells.Count * biome.density);
                var visibleLocations = Info.visibleLocationsByBiome(biome.type);
                if (visibleLocations.Count > 0)
                {
                    for (int i = 0; i < ntries; i++)
                    {
                        Location loc = visibleLocations.Random(rng);
                        var possibleBiomes = loc.biomes.ToList();
                        int biomeNeighbor = 0;
                        int sameNeighbor = 0;
                        foreach (int neighbor in NeighborCell(allCells[i]))
                        {
                            if (possibleBiomes.Contains(this.cells[neighbor]))
                            {
                                biomeNeighbor += 1;
                            }
                            if (this.cells[neighbor] == loc.type)
                            {
                                sameNeighbor += 1;
                            }
                        }
                        if (biomeNeighbor > 0 && sameNeighbor == 0)
                        {
                            AddLocation(allCells[i], loc.type);
                        }
                    }
                }
            }
        }
        void AddAtLeastOnePrison(RNG rng)
        {
            Assert.IsTrue(Info.locations.ContainsKey("prison"));
            if (locations.Values.Contains("prison") == true)
            {
                return;
            }
            foreach (string biome in Info.locations["prison"].biomes)
            {
                foreach (int cell in biomes[biome])
                {
                    if (locations.ContainsKey(cell) == false)
                    {
                        AddLocation(cell, "prison");
                        return;
                    }
                }
            }
            AddLocation(0, "prison");
        }

        public int SeachClosestPrison()
        {
            int best = currentPosition;
            float bestDist = Mathf.Infinity;
            foreach (var pair in locations)
            {
                if (pair.Value != "prison") continue;
                float dist = Distance(currentPosition, pair.Key);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = pair.Key;
                }
            }
            return best;
        }

        (Color color, char c, float size) OriginalChar(int idx)
        {
            Cell cell = Info.cells[cells[idx]];
            return (cell.color, cell.sign, cell.size);
        }


        bool UpdateChar(int idx)
        {
            if (currentStep == Step.CHOOSE_SKILLS) return false;
            CharPixel cp = GetChar(idx);
            Color color;
            char c;
            float size;
            Color white = new Color(1f, 1f, 1f);
            Cell charCell = Info.cells[cells[idx]];
            (color, c, size) = OriginalChar(idx);
            if (currentStep == Step.CHOOSE_DESTINATION && !message.displaying && targetPosition >= 0 && currentPath != null && !Walking())
            {
                if (currentPath.nodes.Contains(idx))
                {
                    int pidx = currentPath.nodes.IndexOf(idx);
                    float risk = RiskAt(idx) / 0.5f;
                    if (risk > 1f) risk = 1f;
                    if ((currentStepCounter % currentPath.nodes.Count) == pidx)
                    {
                        color = new Color(1f, 1f - risk, 1f - risk);
                        c = 'o';
                    }
                    else
                    {
                        color = new Color(1f, 1f, 1f);
                        if (waypoints != null && waypoints.Contains(idx))
                        {
                            c = 'x';
                        }
                        else
                        {
                            c = '‧';
                        }
                    }
                }
                if (targetPosition == idx && currentStepCounter % 2 == 0)
                {
                    c = '+';
                }
            }
            if (idx == currentPosition)
            {
                if (currentStepCounter % 6 >= 2 || currentCell.can_reclick == false)
                {
                    c = '☻';
                    size = 1.4f;
                    color = white;
                }
            }
            if (greenMode)
            {
                color = new Color(color.r / 2f, Mathf.Max(0.4f, color.g * 0.8f), color.b / 2f);

            }
            if (greyMode)
            {
                color = color * 0.75f;
            }
            if (avatar.corrupted && charCell.type == SPECIAL.cureLocation && currentStepCounter % 6 < 2)
            {
                c = '!';
                size = 1.6f;
                color = new Color(1f, 1f, 0f);
            }
            else if (hardAnchorsCache != null && hardAnchorsCache.ContainsKey(idx) && currentStepCounter % 6 < 2 && Service.microworld.currentStep == Microworld.Step.CHOOSE_DESTINATION && (idx != currentPosition || currentCell.can_reclick))
            {
                c = '!';
                size = 1.6f;
                color = new Color(1f, 0f, 0f);
                if (hardAnchorsCache[idx].goal_type == SPECIAL.IdeaType.OPPORTUNITY)
                {
                    color = new Color(1f, 0.5f, 0f);
                }
            }
            else if (greenMode && IsClickable(idx) && softAnchorsCache != null && softAnchorsCache.ContainsKey(idx) && currentStepCounter % 6 < 2 && Service.microworld.currentStep == Microworld.Step.CHOOSE_DESTINATION && (idx != currentPosition || currentCell.can_reclick))
            {
                c = (char)('0' + (char)CellGreenNumber(idx));
                size = 1.3f;
                color = new Color(0.5f, 1f, 0.5f);
            }
            else if (!IsClickable(idx))
            {
                color *= 0.7f;
            }
            bool changed = false;
            if (cp.c != c || cp.txtColor != color || cp.text.fontSize != size)
            {
                changed = true;

            }
            cp.Set(c);
            cp.SetSize(size);
            cp.Color(color);
            return changed;
        }

        int CellGreenNumber(int cellIdx)
        {
            return cellIdx % 10;
        }

        void GreenModeUI(Terminal.PixelView pv)
        {
            // if (this.greenMode == false) return;
            // pv.Color(new Color(0f, pv.g, 0f, pv.a));
            // pv.Background(new Color(0f, pv.bg, 0f, pv.ba));
        }

        void UpdateAllChars()
        {
            int maxUpdate = 2000;
            int counter = 0;
            foreach (int i in rndCharIdxs)
            {
                if (UpdateChar(i))
                {
                    counter++;
                    if (counter > maxUpdate) return;
                }
            }
        }

        float TravelDuration(Biome biome)
        {
            float duration = biome.travel_duration;
            foreach (Item item in avatar.UsingItems(biome))
            {
                if (item.duration_reduction >= 0 && item.duration_reduction <= 1f)
                {
                    if (item.biomes != null && item.biomes.Contains(biome.type))
                    {
                        duration *= item.duration_reduction;
                    }
                }

            }
            return duration;
        }

        public bool CanUseVessel(Biome biome = null)
        {
            if (biome == null) biome = BiomeAt(currentPosition);
            return SPECIAL.canUseVessel.Contains(biome.type);
        }

        public bool Traveling()
        {
            return walk.Count > 0;
        }

        void Walk()
        {
            if (currentStep != Step.TRAVEL) return;
            if (walk.Count == 0)
            {
                narrationCursor.SetCurrentNode(GetEntryNode());
                StartStep(NextStep());
                return;
            }
            currentAccident = null;
            chattingWith = null;
            if (AccidentHappens())
            {
                return;
            }
            if (ChattingHappens())
            {
                return;
            }
            MoveTo(walk[0]);
            Service.audio.Play("walk");
            walk.RemoveAt(0);
            float duration = TravelDuration(BiomeAt(currentPosition));
            if (duration > 0)
            {
                avatar.memory.UpdateRecall(duration);
                avatar.ageInMonths += (float)duration;
                if (avatar.imbuement.remainingMonths > 0)
                {
                    avatar.imbuement.remainingMonths -= duration;
                    if (avatar.imbuement.remainingMonths <= 0)
                    {
                        avatar.imbuement.remainingMonths = 0;
                    }
                }
            }
        }

        void ManualCameraRotation(float angle, bool vertical)
        {
            var axis = cam.transform.right;
            if (!vertical)
            {
                axis = cam.transform.up;
            }
            cam.transform.RotateAround(Vector3.zero, axis, angle);
        }

        void CheckEvent()
        {
            float speed = 2f;
            if (currentStep == Step.CHOOSE_DESTINATION)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    ManualCameraRotation(speed, true);
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    ManualCameraRotation(-speed, true);
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    ManualCameraRotation(-speed, false);
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    ManualCameraRotation(speed, false);
                }
            }
        }

        public bool mainScreen = true;
        public bool greenMode => (avatar.imbuement.item != null && avatar.imbuement.item.green_vision);
        public Dictionary<string, bool> greyModes = new Dictionary<string, bool>();

        public void GreyMode(string name, bool b)
        {
            bool previous = false;
            if (!greyModes.ContainsKey(name))
            {
                greyModes.Add(name, b);
            }
            else
            {
                previous = greyModes[name];
            }
            greyModes[name] = b;
            if (previous != greyModes[name] || b)
            {
                Service.audio.Play("step");
            }
        }
        public bool greyMode
        {
            get
            {
                foreach (bool b in greyModes.Values)
                {
                    if (b) return true;
                }
                return false;
            }
        }
        public bool hideUI = false;

        void Update()
        {
            if (multipleInstanceError)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.F11))  // or any key you want
            {
                Service.microworldUI.SetFullsceen(!Service.microworldUI.isFullscreen);
            }
            if (!mainScreen)
            {
                if (currentStep == Step.CHARACTER_CREATION || currentStep == Step.LOADING || currentStep == Step.DEATH_MSG)
                {
                    Service.music.Play("intro");
                }
                else if (avatar.AgeLabel() == "child")
                {
                    Service.music.Play("child");
                }
                else if (avatar.AgeLabel() == "young")
                {
                    Service.music.Play("young");
                }
                else if (avatar.Years() < 28)
                {
                    Service.music.Play("young_adult");
                }
                else if (avatar.Years() < 38)
                {
                    Service.music.Play("adult");
                }
                else if (avatar.Years() < 48)
                {
                    Service.music.Play("old_adult");
                }
                else if (avatar.Years() < avatar.oldAge)
                {
                    Service.music.Play("old");
                }
                else if (avatar.health > 1)
                {
                    Service.music.Play("very_old");
                }
                else
                {
                    Service.music.Play("elder");
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                mainScreen = true;
                return;
            }
            // if (Input.GetKeyDown(KeyCode.G) && currentStep == Step.CHOOSE_DESTINATION) {
            //     greenMode = !greenMode;
            //     return;
            // }
            if (Input.GetKeyDown(KeyCode.H))
            {
                hideUI = !hideUI;
                return;
            }
            if (currentStep == Step.LOADING && Service.microworldNarrator.ready == true && !mainScreen)
            {
                Debug.Log("READY TO START");
                world.Unhide();
                StartStep(NextStep());
            }
            if (currentStep == Step.LOADING) return;
            CheckEvent();
            if (OnDemandRendering.willCurrentFrameRender == true)
            {
                update = true;
                return;
            }
            if (update)
            {
                if (currentStep == Step.ACTION_SKILL_CHECK_POPUP && ActionNewLocation() != null && lastNewLocationCell >= 0)
                {
                    FocusOn(lastNewLocationCell);
                }
                counter++;
                currentStepCounter++;
                message.Update();
                UpdateAllChars();
                Walk();
            }
            update = false;
        }

        Texture2D CellIcon(int verticeIndex, bool useBiome = false)
        {
            Cell cell = Info.cells[cells[verticeIndex]];
            if (useBiome)
            {
                cell = Info.cells[biomeCells[verticeIndex]];
            }
            return Info.landscapeIcons[cell.icon].Get((uint)verticeIndex);
        }
        public ImageBank PortraitBank(Companion companion)
        {
            List<ImageBank> banks = new List<ImageBank>();
            foreach (string portrait in companion.info.portraits)
            {
                if (Info.portraits[companion.sex].ContainsKey(portrait))
                {
                    banks.Add(Info.portraits[companion.sex][portrait]);
                }
            }
            ImageBank bank = banks[companion.uuid % banks.Count];
            return bank;
        }
        public Texture2D Portrait(Companion companion)
        {
            return PortraitBank(companion).Get((uint)companion.uuid);
        }

        void MoveTo(int verticeIndex)
        {
            if (Service.microworldUI.iconRawImage != null)
            {
                if (walk != null && walk.Count <= 1)
                {
                    Service.microworldUI.iconRawImage.texture = CellIcon(verticeIndex);
                }
                else
                {
                    Service.microworldUI.iconRawImage.texture = CellIcon(verticeIndex, true);
                }
            }
            currentPosition = verticeIndex;
            if (!IsOnFocus(currentPosition))
            {
                FocusOn(NeighborToward(focusPosition, currentPosition));
            }
        }
        void FocusOn(int verticeIndex)
        {
            focusPosition = verticeIndex;
            Vector3 p = mesh.Positions[verticeIndex];
            p.Normalize();
            cam.transform.position = p * camHeight;
            cam.transform.LookAt(Vector3.zero);
        }

        public void ProcessPathResponse(World.Path path, PathRequest request)
        {
            if (_paths == null) _paths = new Dictionary<int, World.Path>();
            if (_paths.ContainsKey(path.nodes[0]))
            {
                _paths.Remove(path.nodes[0]);
            }
            _paths.Add(path.nodes[0], path);
        }


        IEnumerable<IAction> FilterUnrelatedWill(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.required_ideas.Length > 0)
                {
                    if (avatar.IsWillAction(action) == false)
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        IEnumerable<IAction> FilterPrice(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.Price() > 0 && avatar.gold < action.Price())
                {
                    continue;
                }
                if (action.ReversePrice() > 0 && avatar.gold >= action.ReversePrice())
                {
                    continue;
                }
                yield return action;
            }
        }
        IEnumerable<IAction> FilterRequirement(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.requirement == null || action.requirement.Length == 0)
                {
                    yield return action;
                }
                else if (avatar.FillRequirement(action.requirement))
                {
                    yield return action;
                }
            }
        }
        IEnumerable<IAction> FilterAge(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (TargetWill() != null && TargetWill().WillName() == action.WillName())
                {
                    yield return action;
                }
                else if (action.age == SPECIAL.Age.ANY)
                {
                    yield return action;
                }
                else if (action.age == SPECIAL.Age.ADULT)
                {
                    if (avatar.Years() >= Avatar.childhoodEnd)
                    {
                        yield return action;
                    }
                }
                else if (action.age == SPECIAL.Age.YOUNG)
                {
                    if (avatar.Years() < Avatar.adulthoodStart)
                    {
                        yield return action;
                    }
                }
            }
        }
        IEnumerable<IAction> FilterPlan(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (IsPlanAction(action))
                {
                    if (TargetWill() == null || TargetWill().WillName() != action.WillName())
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        public IEnumerable<IAction> FilterFullfilledAspiration(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (IsUniqueWill(action))
                {
                    if (avatar.ContainDoneRecord(action.type) || avatar.ContainGiveUpRecord(action.type))
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        public IEnumerable<IAction> FilterImpossibleDebts(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.isDebt)
                {
                    if (!avatar.DebtPossible())
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        public IEnumerable<IAction> FilterUniqueActions(IEnumerable<IAction> actions)
        {
            foreach (var action in actions)
            {
                if (action.OnlyOnce())
                {
                    if (avatar.ContainDoneRecord(action.type))
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        public IEnumerable<IAction> FilterSameOrigin(IEnumerable<IAction> actions)
        {
            var shuffled = actions.ToList();
            shuffled.Shuffle();
            HashSet<string> returned = new HashSet<string>();
            bool hasReturnedTargetWill = false;
            foreach (var action in shuffled)
            {

                bool isTargetWill = false;
                if (TargetWill() != null && action.WillName() == TargetWill().WillName())
                {
                    isTargetWill = true;
                }

                if (isTargetWill && !hasReturnedTargetWill)
                {
                    if (Info.augmented2original.ContainsKey(action.type))
                    {
                        string original = Info.augmented2original[action.type];
                        if (!returned.Contains(original))
                        {
                            returned.Add(original);
                        }
                    }
                    hasReturnedTargetWill = true;
                    yield return action;
                }
                else if (Info.augmented2original.ContainsKey(action.type))
                {
                    string original = Info.augmented2original[action.type];
                    if (!returned.Contains(original))
                    {
                        returned.Add(original);
                        if (isTargetWill) hasReturnedTargetWill = true;
                        yield return action;
                    }
                }
                else
                {
                    if (isTargetWill) hasReturnedTargetWill = true;
                    yield return action;

                }
            }
        }

        public IEnumerable<NarrationNodeBase> FilterPossibleNodes(IEnumerable<NarrationNodeBase> nodes, bool filterPlan = true)
        {
            List<IAction> actions = new List<IAction>();
            List<NarrationNodeBase> results = new List<NarrationNodeBase>();
            foreach (var node in nodes)
            {
                if (node is IAction)
                {
                    actions.Add(node as IAction);
                }
                else
                {
                    results.Add(node);
                }
            }
            foreach (var act in FilterPossibleActions(actions, filterPlan))
            {
                results.Add(act as NarrationNodeBase);
            }
            return results;
        }

        IEnumerable<IAction> FilterDeadendActions(IEnumerable<IAction> actions, bool filterPlan)
        {
            foreach (var action in actions)
            {
                if (action.DilemmaFollow())
                {
                    if (FilterPossibleActions(action.DilemmaChoicesList(), filterPlan).Count() <= 0)
                    {
                        continue;
                    }
                }
                yield return action;
            }
        }
        public IEnumerable<IAction> FilterPossibleActions(IEnumerable<IAction> actions, bool filterPlan = true)
        {
            actions = FilterUnrelatedWill(actions);
            actions = FilterFullfilledAspiration(actions);
            actions = FilterUniqueActions(actions);
            actions = FilterSameOrigin(actions);
            if (filterPlan)
            {
                actions = FilterPlan(actions);
            }
            actions = FilterPrice(actions);
            actions = FilterRequirement(actions);
            actions = FilterAge(actions);
            actions = FilterDeadendActions(actions, filterPlan);
            actions = FilterImpossibleDebts(actions);
            return actions;
        }

        public string CellMood(int cellIdx)
        {
            Cell cell = Info.cells[cells[cellIdx]];
            return cell.moods[cellIdx % cell.moods.Length];
        }
        public string CurrentCellMood()
        {
            Debug.Log("current cell mood : " + CellMood(currentPosition));
            return CellMood(currentPosition);
        }

        bool resetOpportunities = true;
        Utils.HookableDictionary<int, string> clickableCells = null;

        IEnumerable<IAction> GetAllIdeaActions()
        {
            foreach (string idea in avatar.ideas)
            {
                foreach (Action action in Info.actionByIdea[idea])
                {
                    yield return action;
                }
            }
        }
        IEnumerable<IAction> GetAllRequirementActions()
        {
            foreach (var req in avatar.RequirementInventory())
            {
                if (Info.actionByRequirement.ContainsKey(req.type))
                {
                    foreach (Action action in Info.actionByRequirement[req.type])
                    {
                        yield return action;
                    }
                }
            }
        }
        IEnumerable<IAction> GetAllActionGivingIdeaRequirement()
        {
            List<string> currentRequirement = avatar.RequirementInventory().Select(r => r.type).ToList();
            List<string> needRequirement = new List<string>();
            foreach (var act in GetAllIdeaActions())
            {
                var req = act.requirement;
                if (currentRequirement.Contains(req)) continue;
                needRequirement.Add(req);
            }
            foreach (var req in needRequirement)
            {
                if (Info.actionGivingRequirement.ContainsKey(req))
                {
                    foreach (Action action in Info.actionGivingRequirement[req])
                    {
                        yield return action;
                    }
                }
            }

        }
        void UpdateAnchors()
        {
            clickableCells = FindClickableCells();
            int maxIdeaSoftAnchors = (int)(Math.Max(avatar.willpower, avatar.maxIdeaSoftAnchors * avatar.Youngness));
            if (resetOpportunities == false)
            {
                willAnchors = willAnchors.Where(pair => pair.Value.goal_type != SPECIAL.IdeaType.OPPORTUNITY).ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            else
            {
                resetOpportunities = true;
            }

            var rng = new RNG(avatar.Seed() + (int)(avatar.ageInMonths));
            var rng2 = new RNG(avatar.Seed());
            var allIdeaActions = FilterPossibleActions(GetAllIdeaActions(), false).ToList();
            allIdeaActions = allIdeaActions.OrderBy(item => item.type).ToList();
            allIdeaActions.Shuffle(rng2);
            var allIdeaActionsNoPlan = FilterPossibleActions(GetAllIdeaActions(), true).ToList();
            allIdeaActionsNoPlan = allIdeaActionsNoPlan.OrderBy(item => item.type).ToList();
            allIdeaActionsNoPlan.Shuffle(rng);
            Dictionary<string, List<IAction>> actionsByIdea = new Dictionary<string, List<IAction>>();
            foreach (var a in allIdeaActionsNoPlan)
            {
                if (a.required_ideas == null) continue;
                foreach (string idea in a.required_ideas)
                {
                    if (actionsByIdea.ContainsKey(idea) == false)
                    {
                        actionsByIdea.Add(idea, new List<IAction>());
                    }
                    actionsByIdea[idea].Add(a);
                }
            }
            var allRequirementActionsNoPlan = FilterPossibleActions(GetAllRequirementActions(), true).ToList().Shuffle(rng);
            var allActionsGivingRequirement = FilterPossibleActions(GetAllActionGivingIdeaRequirement(), true).ToList().Shuffle(rng);

            // remove opportunity action to respawn them
            var newWillAnchors =  GetActionsAnchors((Action action) => IsPlanAction(action), allIdeaActions, false, -1, false, rng2);

            // remove actions that are not possible anymore
            willAnchors = willAnchors.Where(pair => newWillAnchors.ContainsValue(pair.Value)).ToDictionary(pair => pair.Key, pair => pair.Value);
            // remove duplicated from new anchors
            newWillAnchors = newWillAnchors.Where(pair => willAnchors.ContainsValue(pair.Value) == false).ToDictionary(pair => pair.Key, pair => pair.Value);
            // remove overlapping anchors
            newWillAnchors = newWillAnchors.Where(pair => willAnchors.ContainsKey(pair.Key) == false).ToDictionary(pair => pair.Key, pair => pair.Value);
            // merge new with old anchors to have anchor position conistancy through time
            willAnchors = willAnchors.Concat(newWillAnchors).ToDictionary(pair => pair.Key, pair => pair.Value); ;

            // only keep one opportunity
            var opportunities = willAnchors.Where(pair => pair.Value.goal_type == SPECIAL.IdeaType.OPPORTUNITY).ToDictionary(pair => pair.Key, pair => pair.Value);
            if (opportunities.Count > 1)
            {
                var randOpp = opportunities.ElementAt(Utils.RandomRange(0, opportunities.Count));
                willAnchors = willAnchors.Where(pair => pair.Value.goal_type != SPECIAL.IdeaType.OPPORTUNITY).ToDictionary(pair => pair.Key, pair => pair.Value);
                willAnchors.Add(randOpp.Key, randOpp.Value);
            }

            requirementAnchors = new Dictionary<int, Action>();
            IEnumerable<KeyValuePair<int, Action>> IterateSoftAnchorsCandidats()
            {
                int cursor = 0;
                bool done = false;
                while (!done)
                {
                    done = true;
                    foreach (var ls in actionsByIdea.Values) {
                        if (ls.Count() > cursor)
                        {
                            done = false;
                            int? anchor = FindAnchor(ls[cursor] as Action, rng, true);
                            if (anchor != null)
                            {
                                yield return new KeyValuePair<int, Action>(anchor.Value, ls[cursor] as Action);
                            }
                        }
                    }
                    cursor++;
                }

                foreach (var pair in GetActionsAnchors(null, allRequirementActionsNoPlan, true, avatar.maxIdeaSoftAnchors - requirementAnchors.Count(), false, rng)) {
                    yield return pair;
                }
                foreach (var pair in GetActionsAnchors(null, allActionsGivingRequirement, true, avatar.maxIdeaSoftAnchors - requirementAnchors.Count(), false, rng)) {
                    yield return pair;
                }
            }
            foreach (var anchor in IterateSoftAnchorsCandidats())
            {
                if (requirementAnchors.ContainsKey(anchor.Key) == false)
                {
                    requirementAnchors.Add(anchor.Key, anchor.Value);
                }
                if (requirementAnchors.Count() >= maxIdeaSoftAnchors) break;
            }

            hardAnchorsCache = HardAnchors();
            softAnchorsCache = SoftAnchors()
            .Where(pair => IsClickable(pair.Key) == true)
            .Where(pair => hardAnchorsCache.ContainsKey(pair.Key) == false)
            // .OrderBy(_ => Utils.RandomDouble())
            .Take(maxIdeaSoftAnchors)
            .ToDictionary(pair => pair.Key, pair => pair.Value);
            if (avatar.corrupted)
            {
                softAnchorsCache = softAnchorsCache
                .Where(pair => cells[pair.Key] != SPECIAL.cureLocation)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
                hardAnchorsCache = hardAnchorsCache
                .Where(pair => cells[pair.Key] != SPECIAL.cureLocation)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            }
        }

        public bool IsHardAnchors(Action action)
        {
            return IsPlanAction(action);
        }
        void RemoveAnchor(int cell)
        {
            if (willAnchors != null && willAnchors.ContainsKey(cell))
            {
                willAnchors.Remove(cell);
            }
            if (requirementAnchors != null && requirementAnchors.ContainsKey(cell))
            {
                requirementAnchors.Remove(cell);
            }
            if (softAnchorsCache != null && softAnchorsCache.ContainsKey(cell))
            {
                softAnchorsCache.Remove(cell);
            }
            if (hardAnchorsCache != null && hardAnchorsCache.ContainsKey(cell))
            {
                hardAnchorsCache.Remove(cell);
            }
        }
        public Dictionary<int, Action> AllAnchors()
        {
            var mergedDict = willAnchors
            .Union(requirementAnchors)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return mergedDict;
        }
        Dictionary<int, Action> hardAnchorsCache = null;
        Dictionary<int, Action> softAnchorsCache = null;
        public Dictionary<int, Action> HardAnchors()
        {
            return AllAnchors().Where(kvp => IsHardAnchors(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        public Dictionary<int, Action> SoftAnchors()
        {
            return AllAnchors().Where(kvp => !IsHardAnchors(kvp.Value)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        Utils.HookableDictionary<int, string> FindClickableCells() {
            Utils.HookableDictionary<int, string> cc = new Utils.HookableDictionary<int, string>();
            foreach (var pair in cells)
            {
                Cell cell = Info.cells[pair.Value];
                if (IsClickable(pair.Key))
                {
                    cc.Add(pair.Key, pair.Value);
                }
            }
            return cc;
        }

        int? FindAnchor(Action action, RNG rng, bool onlyClickable = false)
        {
            List<int> choicesSoft = new List<int>();
            Dictionary<string, bool> cache = new Dictionary<string, bool>();
            var _cells = cells;
            if (onlyClickable) _cells = clickableCells;
            foreach (var pair in _cells.ToList().Shuffle(rng))
            {
                Cell cell = Info.cells[pair.Value];
                if (action.where.Contains(cell.type))
                {
                    if (cache.ContainsKey(pair.Value))
                    {
                        if (cache[pair.Value] == false)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (!Info.narrations[cell.type].GetEntryNode().NarrationPathToAction(true, action, (a1, a2) => a1.WillName() == a2.WillName()))
                        {
                            cache.Add(pair.Value, false);
                            continue;
                        }
                        cache.Add(pair.Value, true);
                    }
                    choicesSoft.Add(pair.Key);
                    if (onlyClickable || IsClickable(pair.Key))
                    {
                        return pair.Key;
                    }
                }
            }
            if (choicesSoft.Count == 0) return null;
            return choicesSoft[0];
        }


        Dictionary<int, Action> GetActionsAnchors(Func<Action, bool> condition, List<IAction> allActions = null, bool onlyClickable = false, int maxAnchors = -1, bool shuffle = true, RNG rng = null)
        {
            Dictionary<int, Action> anchors = new Dictionary<int, Action>();
            if (allActions == null)
            {
                allActions = FilterPossibleActions(Info.actions.Values, true).ToList();
            }
            if (shuffle) allActions.Shuffle(rng);
            foreach (Action action in allActions)
            {
                if (anchors.Count() >= maxAnchors && maxAnchors >= 0) break;
                if (condition == null || condition(action))
                {
                    int? a = FindAnchor(action, rng, onlyClickable);
                    if (a != null && anchors.ContainsKey(a.Value) == false)
                    {
                        anchors.Add(a.Value, action);
                    }
                }
            }
            return anchors;
        }
        public bool IsUniqueWill(IAction action)
        {
            return action.goal_type == SPECIAL.IdeaType.PLAN || action.goal_type == SPECIAL.IdeaType.UNIQUE || action.goal_type == SPECIAL.IdeaType.OPPORTUNITY;
        }
        public bool IsPlanAction(IAction action)
        {
            return action.goal_type == SPECIAL.IdeaType.PLAN || action.goal_type == SPECIAL.IdeaType.CYCLIC || action.goal_type == SPECIAL.IdeaType.OPPORTUNITY;
        }
        Dictionary<int, Action> PlanActionAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors(IsPlanAction, allActions, false, -1, false);
        }
        bool IsActionWithItemRequirementFilled(Action action)
        {
            if (IsPlanAction(action)) return false;
            if (action.requirement != null && action.requirement.Length > 0)
            {
                return true;
            }
            return false;
        }

        bool NeededForWill(string item)
        {
            if (avatar.GetInventory().Contains(item) == true || avatar.HaveCompanions(item) == true)
            {
                return false;
            }
            foreach (string idea in avatar.ideas)
            {
                foreach (var action in Info.actionByIdea[idea])
                {
                    if (action.requirement == item)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        bool IsActionWithIdeaRequirementFilled(Action action)
        {
            if (IsPlanAction(action)) return false;
            if (action.required_ideas.Length > 0)
            {
                if (avatar.IsWillAction(action) == true)
                {
                    return true;
                }
            }
            return false;
        }
        bool GiveNeededRequirement(Action action)
        {
            if (IsPlanAction(action)) return false;
            if (action.items != null && action.items.Count() > 0)
            {
                foreach (var item in action.items)
                {
                    if (!avatar.FillRequirement(item) && NeededForWill(item))
                    {
                        return true;
                    }
                }
            }
            if (action.companion != null && action.companion.Length > 0)
            {
                if (!avatar.FillRequirement(action.companion) && NeededForWill(action.companion))
                {
                    return true;
                }
            }
            return false;
        }
        bool IsActionWithAgeRequirementFilled(Action action)
        {
            if (IsPlanAction(action)) return false;
            if (action.age == SPECIAL.Age.YOUNG)
            {
                return true;
            }
            return false;
        }
        bool IsGoldenAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (action.price <= 0) return false;
            // if (action.price <= avatar.gold/2f) return false;
            return true;
        }
        bool IsRelevantAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (action.age != SPECIAL.Age.YOUNG && avatar.Years() <= Avatar.childhoodEnd) return false;
            foreach (string skill in action.skills)
            {
                if (avatar.skills.ContainsKey(skill) == true)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsNewInspirationAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (avatar.ideas.Count >= avatar.maxIdeas) return false;
            if (action.ideas == null || action.ideas.Count() == 0) return false;
            if (action.age != SPECIAL.Age.YOUNG && avatar.Years() <= Avatar.childhoodEnd) return false;
            foreach (string idea in action.ideas)
            {
                if (avatar.ideas.Contains(idea) == false)
                {
                    return true;
                }
            }
            return false;
        }
        bool IsNewCompanionAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (avatar.companions.Count >= avatar.maxCompanions) return false;
            if (action.companion == null || action.companion.Count() == 0) return false;
            if (action.age != SPECIAL.Age.YOUNG && avatar.Years() <= Avatar.childhoodEnd) return false;
            return true;
        }
        bool IsNewItemAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (action.items == null || action.items.Count() == 0) return false;
            if (action.age != SPECIAL.Age.YOUNG && avatar.Years() <= Avatar.childhoodEnd) return false;
            foreach (string item in action.items)
            {
                if (avatar.GetInventory().Contains(item))
                {
                    return false;
                }
            }
            return true;
        }
        bool GainGoldAction(Action action)
        {
            if (Info.allNextActions.Contains(action)) return false;
            if (action.next != null && action.next.Length > 0) return false;
            if (action.gold <= 0) return false;
            if (action.age != SPECIAL.Age.YOUNG && avatar.Years() <= Avatar.childhoodEnd) return false;
            return true;
        }

        Dictionary<int, Action> ItemRequirementFilledAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsActionWithItemRequirementFilled(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> AgeRequirementFilledAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsActionWithAgeRequirementFilled(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> IdeaRequirementFilledAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsActionWithIdeaRequirementFilled(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> GiveRequirementAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => GiveNeededRequirement(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> GoldenAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsGoldenAction(action), allActions, true, avatar.maxIdeaSoftAnchors*3, false);
        }
        Dictionary<int, Action> RelevantAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsRelevantAction(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> NewInspirationAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsNewInspirationAction(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> NewCompanionAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsNewCompanionAction(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> NewItemAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => IsNewItemAction(action), allActions, true, avatar.maxIdeaSoftAnchors, false);
        }
        Dictionary<int, Action> GainGoldAnchors(List<IAction> allActions)
        {
            return GetActionsAnchors((Action action) => GainGoldAction(action), allActions, true, avatar.maxIdeaSoftAnchors*3, false);
        }
    }
}
