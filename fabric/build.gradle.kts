import com.google.protobuf.gradle.*

val igniteVersion = "2.8.0"
val grpcVersion = "1.31.1"
val grpcKotlinVersion = "0.2.0"
val protobufVersion = "3.13.0"
val coroutinesVersion = "1.3.9"

plugins {
    id("org.gradle.application")
    kotlin("jvm") version "1.4.10"
    id("org.jmailen.kotlinter") version "3.2.0"
    id("com.google.protobuf") version "0.8.13"
}

repositories {
    mavenCentral()
}

dependencies {
    implementation(kotlin("stdlib"))
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:$coroutinesVersion")
    implementation("org.apache.ignite:ignite-core:$igniteVersion")
    implementation("javax.annotation:javax.annotation-api:1.2")
    implementation("com.google.protobuf:protobuf-java-util:$protobufVersion")
    implementation("io.grpc:grpc-protobuf:$grpcVersion")
    implementation("io.grpc:grpc-stub:$grpcVersion")
    implementation("io.grpc:grpc-kotlin-stub:$grpcKotlinVersion")
    runtimeOnly("io.grpc:grpc-netty-shaded:$grpcVersion")
    testImplementation("junit:junit:4.12")
}

application {
    version = "0.1.1"
    mainClass.set("com.obecto.perper.fabric.Main")
    description = "My Application"
}

java {
    sourceCompatibility = JavaVersion.VERSION_1_8
    targetCompatibility = JavaVersion.VERSION_1_8
}

sourceSets {
  main {
    proto {
      srcDir("../protobuf")
    }
  }
}

protobuf {
    protoc {
        artifact = "com.google.protobuf:protoc:$protobufVersion"
    }
    plugins {
        id("grpc") {
            artifact = "io.grpc:protoc-gen-grpc-java:$grpcVersion"
        }
        id("grpckt") {
            artifact = "io.grpc:protoc-gen-grpc-kotlin:$grpcKotlinVersion:jdk7@jar"
        }
    }
    generateProtoTasks {
        ofSourceSet("main").forEach {
            it.plugins {
                id("grpc")
                id("grpckt")
            }
        }
    }
}