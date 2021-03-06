﻿using ColossalFramework.UI;
using UnityEngine;
using System;
using ColossalFramework;
using ColossalFramework.Math;
using PathManagerExtended.Util;
using PathManagerExtended.UI;
using System.Collections.Generic;

namespace PathManagerExtended.Tool
{
    public class PathManagerExtendedTool : DefaultTool
    {
        public static readonly SavedInputKey ActivationShortcut = new SavedInputKey("ActivationShortcut", nameof(PathManagerExtended), SavedInputKey.Encode(KeyCode.P, true, false, false), true);

        public static bool CtrlIsPressed => Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        public static bool ShiftIsPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        public static Ray MouseRay { get; private set; }
        public static float MouseRayLength { get; private set; }
        public static bool MouseRayValid { get; private set; }
        public static Vector3 MousePosition { get; private set; }
        public static Vector3 MouseWorldPosition { get; private set; }

        public BaseTool CurrentTool { get; private set; }
        private Dictionary<ToolType, BaseTool> Tools { get; set; } = new Dictionary<ToolType, BaseTool>();

        public bool ToolEnabled => enabled;
        public SegmentData SegmentInstance { get; private set; } = new SegmentData();
        public LaneData LaneInstance { get; private set; } = new LaneData(0);

        PathManagerExtendedButton Button => PathManagerExtendedButton.Instance;
        PathManagerExtendedPanel Panel => PathManagerExtendedPanel.Instance;

        public static PathManagerExtendedTool Instance { get; set; }

        #region Base Functions
        protected override void Awake()
        {
            Log.Info("LaneManagerTool.Awake()");
            base.Awake();

            Tools = new Dictionary<ToolType, BaseTool>()
            {
                { ToolType.SelectInstance, new SelectInstanceTool() },
                { ToolType.SelectLane, new SelectLaneTool() },
                // More here...
            };

            PathManagerExtendedButton.CreateButton();
            PathManagerExtendedPanel.CreatePanel();

            DisableTool();
        }

        public static PathManagerExtendedTool Create()
        {
            Log.Debug("PathManagerExtendedTool.Create()");
            GameObject toolModControl = ToolsModifierControl.toolController.gameObject;
            Instance = toolModControl.AddComponent<PathManagerExtendedTool>();
            Log.Info($"Tool created");
            return Instance;
        }

        public static void Remove()
        {
            Log.Debug("PathManagerExtendedTool.Remove()");
            if (Instance != null)
            {
                Destroy(Instance);
                Instance = null;
                Log.Info($"Tool removed");
            }
        }

        protected override void OnDestroy()
        {
            Log.Debug("PathManagerExtendedTool.OnDestroy()");
            base.OnDestroy();

            PathManagerExtendedButton.RemoveButton();
            PathManagerExtendedPanel.RemovePanel();

            DisableTool();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Button?.Activate();
            Reset();

            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Button?.Deactivate();
            Reset();
            //LaneManagerPanel.Instance?.Close();
            ToolsModifierControl.SetTool<DefaultTool>();
        }

        public void Reset()
        {
            Panel.Hide();
            SetMode(ToolType.SelectInstance);
            SegmentInstance.Empty();
        }
        public void SetDefaultMode() => SetMode(ToolType.SelectInstance);
        public void SetMode(ToolType mode) => SetMode(Tools[mode]);
        public void SetMode(BaseTool mode)
        {
            CurrentTool?.DeInit();
            CurrentTool = mode;
            CurrentTool?.Init();

            if (CurrentTool?.ShowPanel == true)
                Panel.Show();
            else
                Panel.Hide();
        }

        public void EnableTool()
        {
            Log.Debug("PathManagerExtendedTool.EnableTool()");
            enabled = true;
        }

        public void DisableTool()
        {
            Log.Debug("PathManagerExtendedTool.DisableTool()");
            enabled = false;
        }

        public void ToggleTool()
        {
            Log.Debug("PathManagerExtendedTool.ToggleTool()");
            enabled = !enabled;
        }
        #endregion

        #region Tool Update
        protected override void OnToolUpdate()
        {
            MousePosition = Input.mousePosition;
            MouseRay = Camera.main.ScreenPointToRay(MousePosition);
            MouseRayLength = Camera.main.farClipPlane;
            MouseRayValid = !UIView.IsInsideUI() && Cursor.visible;
            RaycastInput input = new RaycastInput(MouseRay, MouseRayLength);
            RayCast(input, out RaycastOutput output);
            MouseWorldPosition = output.m_hitPos;

            CurrentTool.OnUpdate();

            base.OnToolUpdate();
        }

        public void SetSegment(ushort segmentID)
        {
            SegmentInstance = NetUtil.GetSegmentData(segmentID);
        }

        #endregion

        #region Render Overlay
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            CurrentTool.RenderOverlay(cameraInfo);
            base.RenderOverlay(cameraInfo);
        }

        public static new bool RayCast(RaycastInput input, out RaycastOutput output) => ToolBase.RayCast(input, out output);

        #endregion

        #region Tool GUI
        private bool IsMouseDown { get; set; }
        private bool IsMouseMove { get; set; }
        protected override void OnToolGUI(Event e)
        {
            CurrentTool.OnGUI(e);

            switch (e.type)
            {
                case EventType.MouseDown when MouseRayValid && e.button == 0:
                    IsMouseDown = true;
                    IsMouseMove = false;
                    CurrentTool.OnMouseDown(e);
                    break;
                case EventType.MouseDrag when MouseRayValid:
                    IsMouseMove = true;
                    CurrentTool.OnMouseDrag(e);
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 0:
                    if (IsMouseMove)
                        CurrentTool.OnMouseUp(e);
                    else
                        CurrentTool.OnPrimaryMouseClicked(e);
                    IsMouseDown = false;
                    break;
                case EventType.MouseUp when MouseRayValid && e.button == 1:
                    CurrentTool.OnSecondaryMouseClicked();
                    break;
            }
        }
        #endregion


        /*public ushort HoveredNodeId { get; private set; } = 0;
        public ushort HoveredSegmentId { get; private set; } = 0;
        public int HoveredLaneIndex { get; set; } = 0;
        public Vector3 HitPos { get; private set; }

        public bool IsMouseRayValid => !UIView.IsInsideUI() && Cursor.visible && m_mouseRayValid;
        protected bool HoverValid => IsMouseRayValid && (HoveredSegmentId != 0 || HoveredNodeId != 0);

        private bool DetermineHoveredElements()
        {
            HoveredSegmentId = 0;
            HoveredNodeId = 0;
            HoveredLaneIndex = -1;
            HitPos = Vector3.zero;
            if (!IsMouseRayValid)
                return false;

            // find currently hovered node
            if (ToolMode == Mode.Segment)
            {
                RaycastInput nodeInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
                {
                    m_netService = GetService(),
                    m_ignoreTerrain = true,
                    m_ignoreNodeFlags = NetNode.Flags.None
                };

                if (RayCast(nodeInput, out RaycastOutput nodeOutput))
                {
                    HoveredNodeId = nodeOutput.m_netNode;
                    HitPos = nodeOutput.m_hitPos;
                }

                HoveredSegmentId = GetSegmentFromNode();

                if (HoveredSegmentId != 0)
                {
                    Debug.Assert(HoveredNodeId != 0, "unexpected: HoveredNodeId == 0");
                    return true;
                }
            }

            // find currently hovered segment
            var segmentInput = new RaycastInput(m_mouseRay, m_mouseRayLength)
            {
                m_netService = GetService(),
                m_ignoreTerrain = true,
                m_ignoreSegmentFlags = NetSegment.Flags.None
            };

            if (RayCast(segmentInput, out RaycastOutput segmentOutput))
            {
                HoveredSegmentId = segmentOutput.m_netSegment;
                HitPos = segmentOutput.m_hitPos;
            }

            if (ToolMode == Mode.Lanes && HoveredSegmentId == SegmentInstance.SegmentID)
            {
                if (SegmentInstance.Segment.GetClosestLanePosition(HitPos, NetInfo.LaneType.All, VehicleInfo.VehicleType.All, out _, out uint laneID, out _, out _))
                {
                    for (int i = 0; i < SegmentInstance.Lanes.Count; i++)
                    {
                        if (SegmentInstance.Lanes[i].LaneID == laneID)
                        {
                            HoveredLaneIndex = i;
                            break;
                        }
                    }
                }
            }

            if (ToolMode == Mode.Lane && HoveredLaneIndex == LaneInstance.Index)
            {
                if (SegmentInstance.Segment.GetClosestLanePosition(HitPos, NetInfo.LaneType.All, VehicleInfo.VehicleType.All, out _, out uint laneID, out _, out _))
                {
                    for (int i = 0; i < SegmentInstance.Lanes.Count; i++)
                    {
                        if (SegmentInstance.Lanes[i].LaneID == laneID)
                        {
                            HoveredLaneIndex = i;
                            break;
                        }
                    }
                }
            }

            if (HoveredNodeId <= 0 && HoveredSegmentId > 0)
            {
                // alternative way to get a node hit: check distance to start and end nodes
                // of the segment
                ushort startNodeId = HoveredSegmentId.ToSegment().m_startNode;
                ushort endNodeId = HoveredSegmentId.ToSegment().m_endNode;

                var vStart = segmentOutput.m_hitPos - startNodeId.ToNode().m_position;
                var vEnd = segmentOutput.m_hitPos - endNodeId.ToNode().m_position;

                float startDist = vStart.magnitude;
                float endDist = vEnd.magnitude;

                if (startDist < endDist && startDist < 75f)
                {
                    HoveredNodeId = startNodeId;
                }
                else if (endDist < startDist && endDist < 75f)
                {
                    HoveredNodeId = endNodeId;
                }
            }
            return HoveredNodeId != 0 || HoveredSegmentId != 0;
        }

        static float GetAngle(Vector3 v1, Vector3 v2)
        {
            float ret = Vector3.Angle(v1, v2);
            if (ret > 180) ret -= 180; //future proofing
            ret = Math.Abs(ret);
            return ret;
        }

        internal ushort GetSegmentFromNode()
        {
            bool considerSegmentLenght = false;
            ushort minSegId = 0;
            if (HoveredNodeId != 0)
            {
                NetNode node = HoveredNodeId.ToNode();
                Vector3 dir0 = node.m_position - m_mousePosition;
                float min_angle = float.MaxValue;
                for (int i = 0; i < 8; ++i)
                {
                    ushort segmentId = node.GetSegment(i);
                    if (segmentId == 0)
                        continue;
                    NetSegment segment = segmentId.ToSegment();
                    Vector3 dir;
                    if (segment.m_startNode == HoveredNodeId)
                    {
                        dir = segment.m_startDirection;

                    }
                    else
                    {
                        dir = segment.m_endDirection;
                    }
                    float angle = GetAngle(-dir, dir0);
                    if (considerSegmentLenght)
                        angle *= segment.m_averageLength;
                    if (angle < min_angle)
                    {
                        min_angle = angle;
                        minSegId = segmentId;
                    }
                }
            }
            return minSegId;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);
            if (!enabled)
                return;

            if (!SegmentInstance.IsEmpty)
            {
                //RenderUtil.RenderAutoCutSegmentOverlay(cameraInfo, SegmentInstance.SegmentID, Color.black, false);
            }

            if (ToolMode == Mode.Segment)
            {
                RenderUtil.RenderAutoCutSegmentOverlay(cameraInfo, HoveredSegmentId, Color.white, true);
            }

            if (ToolMode == Mode.Lanes && HoveredLaneIndex > -1)
            {
                RenderUtil.RenderLaneOverlay(cameraInfo, SegmentInstance.Lanes[HoveredLaneIndex], Color.white, true);
            }

            if (ToolMode == Mode.Lane && !LaneInstance.IsEmpty)
            {
                RenderUtil.RenderLaneOverlay(cameraInfo, LaneInstance, Color.magenta, true);


            }
            Panel.Render(cameraInfo);
        }

        public void OnLaneUISelect(int Lane)
        {
            if (ToolMode == Mode.Lanes && !SegmentInstance.IsEmpty)
            {
                if (Lane >= 0 && Lane < SegmentInstance.Lanes.Count)
                {
                    LaneInstance = SegmentInstance.Lanes[Lane];
                    ToolMode = Mode.Lane;
                }
            }
        }

        private void OnPrimaryMouseClicked(Event e)
        {
            Mode pt = ToolMode;
            if (!HoverValid)
                return;
            if (ToolMode == Mode.Segment && HoveredSegmentId != 0)
            {
                Log.Info($"OnPrimaryMouseClicked: segment {HoveredSegmentId} node {HoveredNodeId}");
                SegmentInstance = NetUtil.GetSegmentData(HoveredSegmentId);
                Panel.SetSegment(SegmentInstance.SegmentID);
                ToolMode = Mode.Lanes;
            } else if (ToolMode == Mode.Lanes && HoveredLaneIndex > -1)
            {
                LaneInstance = SegmentInstance.Lanes[HoveredLaneIndex];
                Panel.UpdatePanel();
                ToolMode = Mode.Lane;
            }
            Log.Debug($"Prev ToolMode {pt}, New ToolMode {ToolMode}");
        }

        private void OnSecondaryMouseClicked(Event e)
        {
            Mode pt = ToolMode;
            if (ToolMode == Mode.Segment && SegmentInstance.IsEmpty) {
                DisableTool();
            } else if (ToolMode == Mode.Lanes) {
                SegmentInstance = new SegmentData();
                ToolMode = Mode.Segment;
                Reset();
            } else if (ToolMode == Mode.Lane) {
                LaneInstance = new LaneData(0);
                ToolMode = Mode.Lanes;
                Panel.UpdatePanel();
            } else {

            }
            Log.Debug($"Prev ToolMode {pt}, New ToolMode {ToolMode}");
        }*/
    }
}
