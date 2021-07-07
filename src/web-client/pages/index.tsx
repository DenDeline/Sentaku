import Head from 'next/head'
import Link from 'next/link'
import React from "react";
import {Button} from "@material-ui/core";


const Home: React.FC = (props) => {
    return (
        <>
            <Head>
                <title>Create Next App</title>
                <meta name="description" content="Generated by create next app" />
                <link rel="icon" href="/favicon.ico" />
            </Head>

            <h1>Home page</h1>
            
            <Link href={"/sign-in"} passHref={true}>
                <Button>Test auth</Button>
            </Link>
        </>
    )
};

export default Home;
