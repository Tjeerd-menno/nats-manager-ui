import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  ReactFlow,
  ReactFlowProvider,
  Controls,
  MiniMap,
  Background,
  useNodesState,
  useEdgesState,
  type Node,
  type Edge,
  type NodeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { Badge, Box, Text } from '@mantine/core';
import type { RelationshipMap, ResourceNode } from './types';
import { WarningOverlay } from './components/WarningOverlay';
import { AlertHighlight } from './components/AlertHighlight';

interface RelationshipFlowProps {
  map: RelationshipMap;
  selectedNode: ResourceNode | null;
  onNodeSelect: (node: ResourceNode | null) => void;
  onRecenter: (node: ResourceNode) => void;
}

const COLORS: Record<string, string> = {
  Stream: '#228be6',
  Consumer: '#7950f2',
  KvBucket: '#40c057',
  ObjectBucket: '#fd7e14',
  Service: '#e64980',
  Server: '#868e96',
  Subject: '#fab005',
  Alert: '#fa5252',
  Event: '#f03e3e',
  External: '#ced4da',
};

function buildFlowNodes(rrm: RelationshipMap, selectedNodeId?: string): Node[] {
  const count = rrm.nodes.length;
  return rrm.nodes.map((n, i) => {
    const angle = count > 1 ? (2 * Math.PI * i) / count : 0;
    const radius = Math.max(200, count * 40);
    return {
      id: n.nodeId,
      position: n.isFocal
        ? { x: 0, y: 0 }
        : { x: radius * Math.cos(angle), y: radius * Math.sin(angle) },
        data: {
          label: (
            <Box>
              <Text size="xs" fw={n.isFocal ? 700 : 500}>{n.displayName}</Text>
              <Box mt={4} style={{ display: 'flex', gap: 4, justifyContent: 'center', flexWrap: 'wrap' }}>
                <AlertHighlight resourceType={n.resourceType} />
                <WarningOverlay status={n.status} />
              </Box>
            </Box>
          ),
          node: n,
        },
      style: {
        background: COLORS[n.resourceType] ?? '#868e96',
        color: '#fff',
        border: n.isFocal ? '3px solid #fff' : '1px solid rgba(255,255,255,0.3)',
        borderRadius: 8,
        padding: '6px 10px',
        fontWeight: n.isFocal ? 700 : 400,
        boxShadow: n.nodeId === selectedNodeId ? '0 0 0 3px #fab005' : undefined,
        opacity: n.status === 'Unavailable' ? 0.5 : 1,
      },
      type: 'default',
      ariaLabel: `${n.resourceType} ${n.displayName}${n.isFocal ? ' (focal)' : ''}${n.status !== 'Healthy' ? ` — ${n.status}` : ''}`,
    };
  });
}

function buildFlowEdges(rrm: RelationshipMap): Edge[] {
  return rrm.edges.map((e) => ({
    id: e.edgeId,
    source: e.sourceNodeId,
    target: e.targetNodeId,
    label: e.relationshipType,
    animated: e.observationKind === 'Inferred',
    style: {
      stroke:
        e.confidence === 'High'
          ? '#40c057'
          : e.confidence === 'Medium'
            ? '#fab005'
            : '#868e96',
      strokeDasharray: e.observationKind === 'Inferred' ? '5,3' : undefined,
    },
    data: { edge: e },
  }));
}

function RelationshipFlowInner({
  map,
  selectedNode,
  onNodeSelect,
  onRecenter,
}: RelationshipFlowProps) {
  const initialNodes = useMemo(
    () => buildFlowNodes(map, selectedNode?.nodeId),
    [map, selectedNode?.nodeId]
  );
  const initialEdges = useMemo(() => buildFlowEdges(map), [map]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
  const [announcement, setAnnouncement] = useState('');

  useEffect(() => {
    setNodes(initialNodes);
  }, [initialNodes, setNodes]);

  useEffect(() => {
    setEdges(initialEdges);
  }, [initialEdges, setEdges]);

  const handleNodeClick: NodeMouseHandler = useCallback(
    (_event, flowNode) => {
      const rrmNode = map.nodes.find((n) => n.nodeId === flowNode.id) ?? null;
      onNodeSelect(rrmNode);
    },
    [map.nodes, onNodeSelect]
  );

  const handleNodeDoubleClick: NodeMouseHandler = useCallback(
    (_event, flowNode) => {
      const rrmNode = map.nodes.find((n) => n.nodeId === flowNode.id);
      if (rrmNode) {
        setAnnouncement(`Recentering relationship map on ${rrmNode.displayName}`);
        onRecenter(rrmNode);
      }
    },
    [map.nodes, onRecenter]
  );

  return (
    <div style={{ width: '100%', height: '520px' }}>
      <Text aria-live="polite" className="sr-only" style={{ position: 'absolute', left: -10000 }}>
        {announcement}
      </Text>
      <ReactFlow
        nodes={nodes}
        edges={edges}
        onNodesChange={onNodesChange}
        onEdgesChange={onEdgesChange}
        onNodeClick={handleNodeClick}
        onNodeDoubleClick={handleNodeDoubleClick}
        fitView
        aria-label="Resource relationship graph"
      >
        <Controls />
        <MiniMap />
        <Background />
      </ReactFlow>
    </div>
  );
}

export function RelationshipFlow(props: RelationshipFlowProps) {
  if (props.map.nodes.length === 0) {
    return (
      <Box py="xl" ta="center">
        <Text c="dimmed">No relationships found for this resource.</Text>
        <Badge color="gray" mt="xs">
          {props.map.omittedCounts.filteredNodes} node(s) filtered out
        </Badge>
      </Box>
    );
  }

  return (
    <ReactFlowProvider>
      <RelationshipFlowInner {...props} />
    </ReactFlowProvider>
  );
}
