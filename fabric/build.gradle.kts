val igniteVersion = "2.8.0"

plugins {
            kotlin("jvm") version "1.4.10"
            id("org.jmailen.kotlinter") version "3.2.0"
            application
}

application {
    version = "0.1.1"
    mainClassName = "com.obecto.perper.fabric.Main"
    description = "My Application"
}
java {
    sourceCompatibility = JavaVersion.VERSION_1_8
    targetCompatibility = JavaVersion.VERSION_1_8
}

repositories {
	mavenCentral()
}

dependencies {
	implementation(kotlin("stdlib"))
	implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.3.9")
	implementation("org.apache.ignite:ignite-core:$igniteVersion")
	testImplementation("junit:junit:4.12")
}
