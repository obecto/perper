package agent

type graph struct {
	edges    map[string][]string
	capacity int
	sequence []string
}

func (g *graph) add(key, value string) {
	if key == value {
		g.edges[key] = append(g.edges[key])
		g.capacity++
	} else {
		g.edges[key] = append(g.edges[key], value)
		g.capacity += 2
	}
}

//
type visitedColor int

const (
	white visitedColor = iota
	grey
	black
)

var visitedIndex int
var visited map[string]visitedColor

func (g *graph) topoSortGraph() {
	visitedIndex = 0
	visited = make(map[string]visitedColor, g.capacity)
	g.sequence = make([]string, g.capacity)

	for i := range g.edges {
		visited[i] = white
	}

	index := hasUnvisitedVertex(visited)
	for index != "" {
		g.visit(index)
		index = hasUnvisitedVertex(visited)
	}
	g.sequence = g.sequence[:visitedIndex]
	g.reverseSequence()
}

func (g *graph) visit(index string) {
	visited[index] = grey
	if g.edges != nil {
		for _, v := range g.edges[index] {
			if visited[v] == grey {
				panic("It appears there is a cycle in depending agents!")
			}
			if visited[v] == white {
				g.visit(v)
			}
		}
	}
	visited[index] = black
	g.sequence[visitedIndex] = index
	visitedIndex++
}

func (g *graph) reverseSequence() {
	i := visitedIndex - 1
	j := 0
	for i > j {
		b := g.sequence[i]
		g.sequence[i] = g.sequence[j]
		g.sequence[j] = b
		i--
		j++
	}
}

func hasUnvisitedVertex(visited map[string]visitedColor) string {
	for i, v := range visited {
		if v == white {
			return i
		}
	}
	return ""
}
