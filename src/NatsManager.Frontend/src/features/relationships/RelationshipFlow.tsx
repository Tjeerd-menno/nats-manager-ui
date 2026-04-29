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

function buildFlowNodes(map: RelationshipMap, selectedNodeId?: string): Node[] {
  const count = map.nodes.length;
  return map.nodes.map((node, index) => {
    const angle = count > 1 ? (2 * Math.PI * index) / count : 0;
    const radius = Math.max(200, count * 40);
    return {
      id: node.nodeId,
      position: node.isFocal ? { x: 0, y: 0 } : { x: radius * Math.cos(angle), y: radius * Math.sin(angle) },
      data: {
        label: (
          <Box>
            <Text size="xs" fw={node.isFocal ? 700 : 500}>{node.displayName}</Text>
            <Box mt={4} style={{ display: 'flex', gap: 4, justifyContent: 'center', flexWrap: 'wrap' }}>
              <WarningOverlay status={node.status} />
            </Box>
          </Box>
        ),
        node,
      },
      style: {
        background: COLORS[node.resourceType] ?? '#868e96',
        color: '#fff',
        border: node.isFocal ? '3px solid #fff' : '1px solid rgba(255,255,255,0.3)',
        borderRadius: 8,
        padding: '6px 10px',
        fontWeight: node.isFocal ? 700 : 400,
        boxShadow: node.nodeId === selectedNodeId ? '0 0 0 3px #fab005' : undefined,
        opacity: node.status === 'Unavailable' ? 0.5 : 1,
      },
      type: 'default',
      ariaLabel: `${node.resourceType} ${node.displayName}${node.isFocal ? ' (focal)' : ''}${node.status !== 'Healthy' ? ` — ${node.status}` : ''}`,
    };
  });
}

function buildFlowEdges(map: RelationshipMap): Edge[] {
  return map.edges.map((edge) => ({
    id: edge.edgeId,
    source: edge.sourceNodeId,
    target: edge.targetNodeId,
    label: edge.relationshipType,
    animated: edge.observationKind === 'Inferred',
    style: {
      stroke: edge.confidence === 'High' ? '#40c057' : edge.confidence === 'Medium' ? '#fab005' : '#868e96',
      strokeDasharray: edge.observationKind === 'Inferred' ? '5,3' : undefined,
    },
    data: { edge },
  }));
}

function RelationshipFlowInner({ map, selectedNode, onNodeSelect, onRecenter }: RelationshipFlowProps) {
  const initialNodes = useMemo(() => buildFlowNodes(map, selectedNode?.nodeId), [map, selectedNode?.nodeId]);
  const initialEdges = useMemo(() => buildFlowEdges(map), [map]);

  const [nodes, setNodes, onNodesChange] = useNodesState(initialNodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState(initialEdges);
  const [announcement, setAnnouncement] = useState('');

  useEffect(() => setNodes(initialNodes), [initialNodes, setNodes]);
  useEffect(() => setEdges(initialEdges), [initialEdges, setEdges]);

  const handleNodeClick: NodeMouseHandler = useCallback((_, flowNode) => {
    const relationshipNode = map.nodes.find((node) => node.nodeId === flowNode.id) ?? null;
    onNodeSelect(relationshipNode);
  }, [map.nodes, onNodeSelect]);

  const handleNodeDoubleClick: NodeMouseHandler = useCallback((_, flowNode) => {
    const relationshipNode = map.nodes.find((node) => node.nodeId === flowNode.id);
    if (relationshipNode) {
      setAnnouncement(`Recentering relationship map on ${relationshipNode.displayName}`);
      onRecenter(relationshipNode);
    }
  }, [map.nodes, onRecenter]);

  return (
    <div style={{ width: '100%', height: '520px' }}>
      <Text aria-live="polite" className="sr-only" style={{ position: 'absolute', left: -10000 }}>{announcement}</Text>
      <ReactFlow nodes={nodes} edges={edges} onNodesChange={onNodesChange} onEdgesChange={onEdgesChange} onNodeClick={handleNodeClick} onNodeDoubleClick={handleNodeDoubleClick} fitView aria-label="Resource relationship graph">
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
        <Badge color="gray" mt="xs">{props.map.omittedCounts.filteredNodes} node(s) filtered out</Badge>
      </Box>
    );
  }

  return <ReactFlowProvider><RelationshipFlowInner {...props} /></ReactFlowProvider>;
}
