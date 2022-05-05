module github.com/obecto/perper

go 1.16

require (
	github.com/Microsoft/go-winio v0.5.1 // indirect
	github.com/Microsoft/hcsshim v0.9.2 // indirect
	github.com/compose-spec/compose-go v1.0.9-0.20220101154228-91ed80f52afe
	github.com/containerd/containerd v1.5.9 // indirect
	github.com/docker/cli v20.10.12+incompatible
	github.com/docker/compose/v2 v2.2.3
	github.com/docker/docker v20.10.12+incompatible
	github.com/docker/docker-credential-helpers v0.6.4 // indirect
	github.com/fvbommel/sortorder v1.0.2 // indirect
	github.com/golang/protobuf v1.5.2
	github.com/grpc-ecosystem/go-grpc-middleware v1.3.0 // indirect
	github.com/moby/sys/mount v0.3.0 // indirect
	github.com/prometheus/common v0.28.0 // indirect
	github.com/theupdateframework/notary v0.7.0 // indirect
	golang.org/x/sync v0.0.0-20210220032951-036812b2e83c
	google.golang.org/grpc v1.42.0
	google.golang.org/protobuf v1.27.1
	k8s.io/apimachinery v0.23.0
	k8s.io/client-go v0.23.0
	sigs.k8s.io/structured-merge-diff/v4 v4.2.0 // indirect
	sigs.k8s.io/yaml v1.3.0 // indirect
)

// (via compose)
replace github.com/jaguilar/vt100 => github.com/tonistiigi/vt100 v0.0.0-20190402012908-ad4c4a574305
