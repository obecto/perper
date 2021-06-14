import com.google.protobuf.gradle.*
import java.net.URI

val igniteVersion = "2.10.0"
val grpcVersion = "1.31.1"
val grpcKotlinVersion = "0.2.0"
val protobufVersion = "3.13.0"
val coroutinesVersion = "1.3.9"
val slf4jVersion = "1.7.28"

plugins {
    id("org.gradle.application")
    kotlin("jvm") version "1.4.10"
    id("org.jmailen.kotlinter") version "3.2.0"
    id("com.google.protobuf") version "0.8.13"
}

repositories {
    mavenCentral()
    maven { url = URI("https://kotlin.bintray.com/kotlinx") }
}

dependencies {
    implementation(kotlin("stdlib"))
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:$coroutinesVersion")
    implementation("org.apache.ignite:ignite-core:$igniteVersion")
    runtimeOnly("org.apache.ignite:ignite-indexing:$igniteVersion")
    implementation("org.apache.ignite:ignite-slf4j:$igniteVersion")
    runtimeOnly("org.slf4j:slf4j-simple:$slf4jVersion")
    implementation("javax.annotation:javax.annotation-api:1.2")
    implementation("com.google.protobuf:protobuf-java-util:$protobufVersion")
    implementation("io.grpc:grpc-protobuf:$grpcVersion")
    implementation("io.grpc:grpc-stub:$grpcVersion")
    implementation("io.grpc:grpc-kotlin-stub:$grpcKotlinVersion")
    runtimeOnly("io.grpc:grpc-netty-shaded:$grpcVersion")
    implementation("org.apache.commons:commons-lang3:3.11")
    implementation("org.jetbrains.kotlinx:kotlinx-cli:0.3")
    testImplementation("junit:junit:4.12")
}

application {
    version = "0.6.0-alpha7"
    mainClass.set("com.obecto.perper.fabric.Main")
    description = "Perper Fabric"
}

java {
    sourceCompatibility = JavaVersion.VERSION_1_8
    targetCompatibility = JavaVersion.VERSION_1_8
}

sourceSets {
  main {
    proto {
      srcDir("../proto")
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
